import 'dart:async';

import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../auth_providers.dart';
import '../../domain/user_session.dart';
import '../../../workouts/data/workout_local_data_source.dart';

final sessionControllerProvider =
    StateNotifierProvider<SessionController, SessionState>((ref) {
  return SessionController(ref);
});

enum SessionStatus { restoring, guest, authenticated, onboardingRequired }

class SessionState {
  const SessionState({required this.status, this.user});

  const SessionState.guest()
      : status = SessionStatus.guest,
        user = null;

  const SessionState.restoring()
      : status = SessionStatus.restoring,
        user = null;

  final SessionStatus status;
  final UserSession? user;

  bool get isRestoring => status == SessionStatus.restoring;
  bool get isAuthenticated =>
      status == SessionStatus.authenticated ||
      status == SessionStatus.onboardingRequired;
  bool get requiresOnboarding => status == SessionStatus.onboardingRequired;
}

class SessionController extends StateNotifier<SessionState> {
  SessionController(this._ref) : super(const SessionState.restoring()) {
    unawaited(_restoreSession());
  }

  final Ref _ref;

  void startSession(UserSession user, {bool requiresOnboarding = true}) {
    state = SessionState(
      status: requiresOnboarding
          ? SessionStatus.onboardingRequired
          : SessionStatus.authenticated,
      user: user,
    );
  }

  Future<void> completeOnboarding() async {
    state = SessionState(status: SessionStatus.authenticated, user: state.user);
    final user = state.user;
    if (user != null) {
      await _ref.read(authRepositoryProvider).persistSession(
            user,
            requiresOnboarding: false,
          );
    }
  }

  Future<void> _restoreSession() async {
    try {
      final stored = await _ref.read(authRepositoryProvider).restoreSession();
      if (!mounted) {
        return;
      }

      if (stored == null) {
        state = const SessionState.guest();
        return;
      }

      state = SessionState(
        status: stored.requiresOnboarding
            ? SessionStatus.onboardingRequired
            : SessionStatus.authenticated,
        user: stored.user,
      );
    } catch (_) {
      if (mounted) {
        state = const SessionState.guest();
      }
    }
  }

  Future<void> signOut() async {
    final currentUser = state.user;
    state = const SessionState.guest();
    if (currentUser != null) {
      await _ref.read(workoutLocalDataSourceProvider).clearPrivateData(
            tenantId: currentUser.tenantId,
            usuarioId: currentUser.id,
          );
    }

    await _ref.read(authRepositoryProvider).signOut();
    state = const SessionState.guest();
  }
}
