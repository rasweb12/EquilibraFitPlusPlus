import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../core/http/api_client.dart';
import '../../core/storage/secure_token_store.dart';
import 'data/auth_api.dart';
import 'data/auth_repository_impl.dart';
import 'domain/auth_repository.dart';

final authRepositoryProvider = Provider<AuthRepository>((ref) {
  return AuthRepositoryImpl(
    api: AuthApi(ref.watch(apiClientProvider)),
    tokenStore: ref.watch(secureTokenStoreProvider),
  );
});
