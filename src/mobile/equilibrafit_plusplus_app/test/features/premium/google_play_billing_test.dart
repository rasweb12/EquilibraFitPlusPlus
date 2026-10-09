import 'dart:async';

import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/errors/app_failure.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/premium/data/google_play_billing.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/foundation.dart';
import 'package:flutter/services.dart';
import 'package:in_app_purchase/in_app_purchase.dart';

void main() {
  setUp(() => debugDefaultTargetPlatformOverride = TargetPlatform.android);
  tearDown(() => debugDefaultTargetPlatformOverride = null);
  test('premium status reads the existing API contract', () {
    expect(premiumFromStatus({'premiumAtivo': true}), isTrue);
    expect(
      premiumFromStatus({'premiumAtivo': false, 'premium': true}),
      isFalse,
    );
    expect(premiumFromStatus({}), isFalse);
  });

  test('restored purchase is completed only after server verification',
      () async {
    final dio = Dio();
    RequestOptions? captured;
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (request, handler) {
          captured = request;
          handler.resolve(
            Response<Object?>(
              requestOptions: request,
              statusCode: 200,
              data: {'premium': true, 'productId': 'premium.monthly'},
            ),
          );
        },
      ),
    );
    final purchase = _purchase();
    var completed = false;
    final premium = await GooglePlayPurchaseVerifier(ApiClient(dio), 'user-a')
        .verify(purchase, (_) async {
      expect(captured, isNotNull);
      completed = true;
    });
    expect(premium, isTrue);
    expect(completed, isTrue);
    expect(captured?.path, '/api/v1/billing/google-play/verify');
    expect(captured?.headers['X-Local-Expected-User'], 'user-a');
    expect(captured?.data, {'purchaseToken': 'test-receipt'});
  });

  test('failed verification never completes a purchase or grants premium',
      () async {
    final dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (request, handler) {
          handler.reject(
            DioException(
              requestOptions: request,
              response: Response<Object?>(
                requestOptions: request,
                statusCode: 400,
                data: {'message': 'Invalid receipt'},
              ),
            ),
          );
        },
      ),
    );
    var completed = false;
    await expectLater(
      GooglePlayPurchaseVerifier(ApiClient(dio), 'user-a').verify(_purchase(),
          (_) async {
        completed = true;
      }),
      throwsA(isA<AppFailure>()),
    );
    expect(completed, isFalse);
  });

  test('billing account identifier is stable and does not expose the UUID', () {
    const user = '12345678-1234-1234-1234-123456789abc';
    expect(billingAccountId(user), hasLength(64));
    expect(billingAccountId(user), billingAccountId(user.toUpperCase()));
    expect(billingAccountId(user), isNot(contains(user)));
  });

  for (final invalid in <Map<String, Object?>>[
    {},
    {'premium': 'true', 'productId': 'premium.monthly'},
    {'premium': true, 'productId': 'another.product'},
  ]) {
    test('invalid server entitlement never completes a purchase: $invalid',
        () async {
      final dio = Dio();
      dio.interceptors.add(
        InterceptorsWrapper(
          onRequest: (request, handler) {
            handler.resolve(
              Response<Object?>(
                requestOptions: request,
                statusCode: 200,
                data: invalid,
              ),
            );
          },
        ),
      );
      var completed = false;
      await expectLater(
        GooglePlayPurchaseVerifier(ApiClient(dio), 'user-a').verify(_purchase(),
            (_) async {
          completed = true;
        }),
        throwsA(
          isA<AppFailure>()
              .having((e) => e.code, 'code', 'billing.invalid_response'),
        ),
      );
      expect(completed, isFalse);
    });
  }

  test('empty receipt is rejected before contacting the server', () async {
    var requested = false;
    final dio = Dio();
    dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (request, handler) {
          requested = true;
          handler.resolve(
            Response<Object?>(requestOptions: request, statusCode: 200),
          );
        },
      ),
    );
    await expectLater(
      GooglePlayPurchaseVerifier(ApiClient(dio), 'user-a')
          .verify(_purchase(receipt: ''), (_) async {
        fail('Must not complete');
      }),
      throwsA(isA<AppFailure>()),
    );
    expect(requested, isFalse);
  });

  test('store failure preserves server premium without crashing load',
      () async {
    final store = _StoreFake()
      ..availabilityError = PlatformException(code: 'private-detail');
    final billing = await _billing(store);
    expect(billing.state.premium, isTrue);
    expect(billing.state.available, isFalse);
    expect(billing.state.loading, isFalse);
    expect(billing.state.message, isNot(contains('private-detail')));
  });

  test('purchase launch failure is recoverable and does not lose premium',
      () async {
    final store = _StoreFake()
      ..purchaseError = PlatformException(code: 'private-detail');
    final billing = await _billing(store);
    await billing.buy(_product());
    expect(billing.state.premium, isTrue);
    expect(billing.state.loading, isFalse);
    expect(billing.state.message, 'Nao foi possivel iniciar a compra.');
  });

  test('restore failure is recoverable and does not lose premium', () async {
    final store = _StoreFake()
      ..restoreError = PlatformException(code: 'private-detail');
    final billing = await _billing(store);
    await billing.restore();
    expect(billing.state.premium, isTrue);
    expect(billing.state.loading, isFalse);
    expect(billing.state.message, isNot(contains('private-detail')));
    expect(billing.state.message, contains('restaurar'));
  });

  test('disabled billing never invokes purchase or restore in the store',
      () async {
    final store = _StoreFake();
    final billing = await _billing(store, enabled: false);
    await billing.buy(_product());
    await billing.restore();
    expect(store.purchaseCalls, 0);
    expect(store.restoreCalls, 0);
    expect(billing.state.premium, isTrue);
  });

  test('concurrent purchase taps initiate only one store request', () async {
    final pending = Completer<bool>();
    final store = _StoreFake()..purchaseResult = pending.future;
    final billing = await _billing(store);
    final first = billing.buy(_product());
    await billing.buy(_product());
    expect(store.purchaseCalls, 1);
    pending.complete(false);
    await first;
    expect(billing.state.loading, isFalse);
    expect(billing.state.message, 'Nao foi possivel iniciar a compra.');
  });

  test('purchase stream errors preserve premium and permit reloading',
      () async {
    final store = _StoreFake();
    final billing = await _billing(store);
    store.purchases.addError(PlatformException(code: 'private-detail'));
    await pumpEventQueue();
    expect(billing.state.premium, isTrue);
    expect(billing.state.available, isFalse);
    await billing.load();
    expect(billing.state.available, isTrue);
    expect(billing.state.message, isNull);
  });
}

PurchaseDetails _purchase({String receipt = 'test-receipt'}) => PurchaseDetails(
      purchaseID: 'order-test',
      productID: 'premium.monthly',
      verificationData: PurchaseVerificationData(
        localVerificationData: '',
        serverVerificationData: receipt,
        source: 'google_play',
      ),
      transactionDate: null,
      status: PurchaseStatus.restored,
    )..pendingCompletePurchase = true;

ProductDetails _product() => ProductDetails(
      id: 'premium.monthly',
      title: 'Premium',
      description: 'Plano de teste',
      price: 'R\$ 10,00',
      rawPrice: 10,
      currencyCode: 'BRL',
    );

Future<GooglePlayBilling> _billing(
  _StoreFake store, {
  bool enabled = true,
}) async {
  final dio = Dio();
  dio.interceptors.add(
    InterceptorsWrapper(
      onRequest: (request, handler) {
        handler.resolve(
          Response<Object?>(
            requestOptions: request,
            statusCode: 200,
            data: request.path.endsWith('/status')
                ? {'premiumAtivo': true}
                : {
                    'enabled': enabled,
                    'productIds': ['premium.monthly'],
                  },
          ),
        );
      },
    ),
  );
  addTearDown(store.purchases.close);
  final billing = GooglePlayBilling(ApiClient(dio), 'user-a', store: store);
  addTearDown(billing.dispose);
  await pumpEventQueue();
  return billing;
}

class _StoreFake implements InAppPurchase {
  final purchases = StreamController<List<PurchaseDetails>>.broadcast();
  Exception? availabilityError;
  Exception? purchaseError;
  Exception? restoreError;
  Future<bool>? purchaseResult;
  int purchaseCalls = 0;
  int restoreCalls = 0;

  @override
  Stream<List<PurchaseDetails>> get purchaseStream => purchases.stream;

  @override
  Future<bool> isAvailable() async {
    if (availabilityError != null) throw availabilityError!;
    return true;
  }

  @override
  Future<ProductDetailsResponse> queryProductDetails(
    Set<String> identifiers,
  ) async =>
      ProductDetailsResponse(productDetails: [_product()], notFoundIDs: []);

  @override
  Future<bool> buyNonConsumable({required PurchaseParam purchaseParam}) async {
    purchaseCalls++;
    if (purchaseError != null) throw purchaseError!;
    return purchaseResult ?? true;
  }

  @override
  Future<void> restorePurchases({String? applicationUserName}) async {
    restoreCalls++;
    if (restoreError != null) throw restoreError!;
  }

  @override
  Future<void> completePurchase(PurchaseDetails purchase) async {}

  @override
  dynamic noSuchMethod(Invocation invocation) =>
      throw UnsupportedError(invocation.memberName.toString());
}
