import 'dart:async';
import 'dart:convert';

import 'package:crypto/crypto.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:in_app_purchase/in_app_purchase.dart';

import '../../../core/errors/app_failure.dart';
import '../../../core/http/api_client.dart';
import '../../../core/sync/sync_owner.dart';

final googlePlayBillingProvider =
    StateNotifierProvider<GooglePlayBilling, BillingState>((ref) {
  final owner = ref.watch(syncOwnerProvider);
  return GooglePlayBilling(ref.watch(apiClientProvider), owner?.id);
});

class BillingState {
  const BillingState({
    this.loading = false,
    this.available = false,
    this.premium = false,
    this.products = const [],
    this.message,
  });
  final bool loading;
  final bool available;
  final bool premium;
  final List<ProductDetails> products;
  final String? message;

  BillingState copyWith({
    bool? loading,
    bool? available,
    bool? premium,
    List<ProductDetails>? products,
    String? message,
  }) =>
      BillingState(
        loading: loading ?? this.loading,
        available: available ?? this.available,
        premium: premium ?? this.premium,
        products: products ?? this.products,
        message: message,
      );
}

class GooglePlayPurchaseVerifier {
  const GooglePlayPurchaseVerifier(this.client, this.userId);
  final ApiClient client;
  final String userId;

  Future<bool> verify(
    PurchaseDetails purchase,
    Future<void> Function(PurchaseDetails) complete,
  ) async {
    if (purchase.verificationData.serverVerificationData.isEmpty) {
      throw const AppFailure(
        'Comprovante de compra indisponivel.',
        code: 'billing.invalid_purchase',
      );
    }
    final result = await client.postJson(
      '/api/v1/billing/google-play/verify',
      body: <String, Object?>{
        'purchaseToken': purchase.verificationData.serverVerificationData,
      },
      headers: <String, Object?>{
        'X-Local-Expected-User': userId,
      },
    );
    if (result['premium'] is! bool ||
        result['productId'] != purchase.productID) {
      throw const AppFailure(
        'Nao foi possivel confirmar esta assinatura.',
        code: 'billing.invalid_response',
      );
    }
    if (purchase.pendingCompletePurchase) await complete(purchase);
    return result['premium'] as bool;
  }
}

class GooglePlayBilling extends StateNotifier<BillingState> {
  GooglePlayBilling(this._client, this._userId, {InAppPurchase? store})
      : _store = store,
        super(const BillingState()) {
    if (!kIsWeb &&
        defaultTargetPlatform == TargetPlatform.android &&
        _userId != null) {
      _store ??= InAppPurchase.instance;
      _subscription = _store!.purchaseStream.listen(
        (purchases) => unawaited(_process(purchases)),
        onError: (_) {
          if (mounted) {
            state = state.copyWith(
              loading: false,
              available: false,
              message: 'Nao foi possivel consultar as compras.',
            );
          }
        },
      );
      unawaited(load());
    }
  }
  final ApiClient _client;
  final String? _userId;
  InAppPurchase? _store;
  StreamSubscription<List<PurchaseDetails>>? _subscription;
  Future<void> _processing = Future<void>.value();

  Future<void> load() async {
    if (_subscription == null || state.loading) return;
    state = state.copyWith(loading: true, available: false, products: const []);
    try {
      final status = await _client.getJson('/api/v1/premium/status');
      if (!mounted) return;
      state = state.copyWith(premium: premiumFromStatus(status));
      final config =
          await _client.getJson('/api/v1/billing/google-play/products');
      if (!mounted) return;
      if (config['enabled'] != true || !await _store!.isAvailable()) {
        if (mounted) {
          state = state.copyWith(
            loading: false,
            message: 'Assinaturas indisponiveis.',
          );
        }
        return;
      }
      final ids = (config['productIds'] as List).cast<String>().toSet();
      if (ids.isEmpty) {
        if (mounted) {
          state = state.copyWith(
            loading: false,
            message: 'Nenhum plano disponivel no momento.',
          );
        }
        return;
      }
      final response = await _store!.queryProductDetails(ids);
      if (mounted) {
        state = state.copyWith(
          loading: false,
          available: response.error == null,
          products: response.productDetails,
          message: response.error != null
              ? 'Nao foi possivel consultar os planos.'
              : response.productDetails.isEmpty
                  ? 'Nenhum plano disponivel no momento.'
                  : response.notFoundIDs.isEmpty
                      ? null
                      : 'Alguns planos estao indisponiveis.',
        );
      }
    } on AppFailure catch (failure) {
      if (mounted) {
        state = state.copyWith(loading: false, message: failure.message);
      }
    } catch (_) {
      if (mounted) {
        state = state.copyWith(
          loading: false,
          available: false,
          message: 'Nao foi possivel consultar a Google Play. Tente novamente.',
        );
      }
    }
  }

  Future<void> buy(ProductDetails product) async {
    if (_userId == null ||
        !state.available ||
        state.loading ||
        !state.products.any((available) => available.id == product.id)) {
      return;
    }
    state = state.copyWith(loading: true);
    try {
      final started = await _store!.buyNonConsumable(
        purchaseParam: PurchaseParam(
          productDetails: product,
          applicationUserName: billingAccountId(_userId),
        ),
      );
      if (mounted && !started) {
        state = state.copyWith(message: 'Nao foi possivel iniciar a compra.');
      }
    } catch (_) {
      if (mounted) {
        state = state.copyWith(message: 'Nao foi possivel iniciar a compra.');
      }
    } finally {
      if (mounted) {
        state = state.copyWith(loading: false, message: state.message);
      }
    }
  }

  Future<void> restore() async {
    if (_subscription == null || !state.available || state.loading) return;
    state = state.copyWith(loading: true);
    try {
      await _store!.restorePurchases(
        applicationUserName: billingAccountId(_userId!),
      );
    } catch (_) {
      if (mounted) {
        state = state.copyWith(
          message: 'Nao foi possivel restaurar as compras. Tente novamente.',
        );
      }
    } finally {
      if (mounted) {
        state = state.copyWith(loading: false, message: state.message);
      }
    }
  }

  Future<void> _process(List<PurchaseDetails> purchases) {
    // Serialize stream batches so restoration and a new purchase cannot overlap.
    _processing = _processing.then((_) async {
      for (final purchase in purchases) {
        if (!mounted) return;
        if (purchase.status == PurchaseStatus.pending) {
          state = state.copyWith(
            message: 'Compra pendente.',
          );
        } else if (purchase.status == PurchaseStatus.purchased ||
            purchase.status == PurchaseStatus.restored) {
          try {
            final premium = await GooglePlayPurchaseVerifier(_client, _userId!)
                .verify(purchase, _store!.completePurchase);
            if (mounted) {
              state = state.copyWith(
                premium: premium,
                message: premium
                    ? 'Premium ativo.'
                    : 'Assinatura sem acesso Premium.',
              );
            }
          } on AppFailure catch (failure) {
            if (mounted) {
              state = state.copyWith(
                message: failure.message,
              );
            }
          } catch (_) {
            if (mounted) {
              state = state.copyWith(
                message:
                    'Confirmacao pendente. Restaure suas compras para tentar novamente.',
              );
            }
          }
        } else if (purchase.status == PurchaseStatus.error ||
            purchase.status == PurchaseStatus.canceled) {
          state = state.copyWith(
            message: purchase.status == PurchaseStatus.canceled
                ? 'Compra cancelada.'
                : 'Nao foi possivel concluir a compra.',
          );
        }
      }
    });
    return _processing;
  }

  @override
  void dispose() {
    _subscription?.cancel();
    super.dispose();
  }
}

String billingAccountId(String userId) =>
    sha256.convert(utf8.encode(userId.toLowerCase())).toString();

bool premiumFromStatus(Map<String, Object?> status) =>
    status['premiumAtivo'] == true;
