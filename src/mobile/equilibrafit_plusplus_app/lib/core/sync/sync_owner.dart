import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../features/auth/domain/user_session.dart';
import '../../features/auth/presentation/controllers/session_controller.dart';

final syncOwnerProvider = Provider<UserSession?>((ref) {
  final session = ref.watch(sessionControllerProvider);
  return session.isAuthenticated ? session.user : null;
});
