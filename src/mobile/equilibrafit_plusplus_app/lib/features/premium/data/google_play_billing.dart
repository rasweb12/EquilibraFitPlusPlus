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
}

class GooglePlayPurchaseVerifier {
  const GooglePlayPurchaseVerifier(this.client, this.userId);
  final ApiClient client;
  final String userId;

  Future<bool> verify(
    PurchaseDetails purchase,
    Future<void> Function(PurchaseDetails) complete,
  ) async {
    final result = await client.postJson(
      '/api/v1/billing/google-play/verify',
      body: <String, Object?>{
        'purchaseToken': purchase.verificationData.serverVerificationData,
      },
      headers: <String, Object?>{
        'X-Local-Expected-User': userId,
      },
    );
    if (purchase.pendingCompletePurchase) await complete(purchase);
    return result['premium'] == true;
  }
}

class GooglePlayBilling extends StateNotifier<BillingState> {
  GooglePlayBilling(this._client, this._userId) : super(const BillingState()) {
    if (!kIsWeb &&
        defaultTargetPlatform == TargetPlatform.android &&
        _userId != null) {
      _subscription = InAppPurchase.instance.purchaseStream.listen(
        (purchases) => unawaited(_process(purchases)),
        onError: (_) {
          if (mounted) {
            state = const BillingState(
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
  StreamSubscription<List<PurchaseDetails>>? _subscription;
  Future<void> _processing = Future<void>.value();

  Future<void> load() async {
    if (_subscription == null) return;
    state = const BillingState(loading: true);
    try {
      final status = await _client.getJson('/api/v1/premium/status');
      final config =
          await _client.getJson('/api/v1/billing/google-play/products');
      if (config['enabled'] != true ||
          !await InAppPurchase.instance.isAvailable()) {
        if (mounted) {
          state = BillingState(
            premium: premiumFromStatus(status),
            message: 'Assinaturas indisponiveis.',
          );
        }
        return;
      }
      final ids = (config['productIds'] as List).cast<String>().toSet();
      final response = await InAppPurchase.instance.queryProductDetails(ids);
      if (mounted) {
        state = BillingState(
          available: response.error == null,
          premium: premiumFromStatus(status),
          products: response.productDetails,
          message: response.notFoundIDs.isEmpty
              ? null
              : 'Alguns planos estao indisponiveis.',
        );
      }
    } on AppFailure catch (failure) {
      if (mounted) state = BillingState(message: failure.message);
    }
  }

  Future<void> buy(ProductDetails product) async {
    if (_userId == null || !state.available) return;
    await InAppPurchase.instance.buyNonConsumable(
      purchaseParam: PurchaseParam(
        productDetails: product,
        applicationUserName: billingAccountId(_userId),
      ),
    );
  }

  Future<void> restore() async {
    if (_subscription == null) return;
    await InAppPurchase.instance
        .restorePurchases(applicationUserName: billingAccountId(_userId!));
  }

  Future<void> _process(List<PurchaseDetails> purchases) {
    // Serialize stream batches so restoration and a new purchase cannot overlap.
    _processing = _processing.then((_) async {
      for (final purchase in purchases) {
        if (!mounted) return;
        if (purchase.status == PurchaseStatus.pending) {
          state = BillingState(
            available: state.available,
            products: state.products,
            premium: state.premium,
            message: 'Compra pendente.',
          );
        } else if (purchase.status == PurchaseStatus.purchased ||
            purchase.status == PurchaseStatus.restored) {
          try {
            final premium = await GooglePlayPurchaseVerifier(_client, _userId!)
                .verify(purchase, InAppPurchase.instance.completePurchase);
            if (mounted) {
              state = BillingState(
                available: state.available,
                products: state.products,
                premium: premium,
                message: premium
                    ? 'Premium ativo.'
                    : 'Assinatura sem acesso Premium.',
              );
            }
          } on AppFailure catch (failure) {
            if (mounted) {
              state = BillingState(
                available: state.available,
                products: state.products,
                premium: state.premium,
                message: failure.message,
              );
            }
          } catch (_) {
            if (mounted) {
              state = BillingState(
                products: state.products,
                message:
                    'Confirmacao pendente. Restaure suas compras para tentar novamente.',
              );
            }
          }
        } else if (purchase.status == PurchaseStatus.error ||
            purchase.status == PurchaseStatus.canceled) {
          state = BillingState(
            available: state.available,
            products: state.products,
            premium: state.premium,
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
