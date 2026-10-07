import 'package:dio/dio.dart';
import 'package:equilibrafit_plusplus_app/core/errors/app_failure.dart';
import 'package:equilibrafit_plusplus_app/core/http/api_client.dart';
import 'package:equilibrafit_plusplus_app/features/premium/data/google_play_billing.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:in_app_purchase/in_app_purchase.dart';

void main() {
  test('premium status reads the existing API contract', () {
    expect(premiumFromStatus({'premiumAtivo': true}), isTrue);
    expect(
        premiumFromStatus({'premiumAtivo': false, 'premium': true}), isFalse,);
    expect(premiumFromStatus({}), isFalse);
  });

  test('restored purchase is completed only after server verification',
      () async {
    final dio = Dio();
    RequestOptions? captured;
    dio.interceptors.add(InterceptorsWrapper(onRequest: (request, handler) {
      captured = request;
      handler.resolve(Response<Object?>(
          requestOptions: request, statusCode: 200, data: {'premium': true},),);
    },),);
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
    dio.interceptors.add(InterceptorsWrapper(onRequest: (request, handler) {
      handler.reject(DioException(
          requestOptions: request,
          response: Response<Object?>(
              requestOptions: request,
              statusCode: 400,
              data: {'message': 'Invalid receipt'},),),);
    },),);
    var completed = false;
    await expectLater(
        GooglePlayPurchaseVerifier(ApiClient(dio), 'user-a').verify(_purchase(),
            (_) async {
          completed = true;
        }),
        throwsA(isA<AppFailure>()),);
    expect(completed, isFalse);
  });

  test('billing account identifier is stable and does not expose the UUID', () {
    const user = '12345678-1234-1234-1234-123456789abc';
    expect(billingAccountId(user), hasLength(64));
    expect(billingAccountId(user), billingAccountId(user.toUpperCase()));
    expect(billingAccountId(user), isNot(contains(user)));
  });
}

PurchaseDetails _purchase() => PurchaseDetails(
    purchaseID: 'order-test',
    productID: 'premium.monthly',
    verificationData: PurchaseVerificationData(
        localVerificationData: '',
        serverVerificationData: 'test-receipt',
        source: 'google_play',),
    transactionDate: null,
    status: PurchaseStatus.restored,)
  ..pendingCompletePurchase = true;
