import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../features/auth/presentation/controllers/session_controller.dart';
import '../features/auth/presentation/pages/forgot_password_page.dart';
import '../features/auth/presentation/pages/login_page.dart';
import '../features/auth/presentation/pages/register_page.dart';
import '../features/auth/presentation/pages/reset_password_page.dart';
import '../features/auth/presentation/pages/splash_page.dart';
import '../features/auth/presentation/pages/welcome_page.dart';
import '../features/coach/presentation/pages/coach_page.dart';
import '../features/dashboard/presentation/pages/dashboard_page.dart';
import '../features/meals/presentation/pages/meal_editor_page.dart';
import '../features/meals/presentation/pages/meal_photo_picker_page.dart';
import '../features/meals/presentation/pages/meals_page.dart';
import '../features/notifications/presentation/pages/notifications_page.dart';
import '../features/onboarding/presentation/pages/onboarding_editor_page.dart';
import '../features/plan/presentation/pages/plan_page.dart';
import '../features/profile/presentation/pages/profile_home_page.dart';
import '../features/premium/presentation/pages/premium_page.dart';
import '../features/progress/presentation/pages/progress_page.dart';
import '../features/sync/presentation/pages/sync_page.dart';
import '../features/workouts/presentation/pages/workouts_page.dart';

final routerProvider = Provider<GoRouter>((ref) {
  final session = ref.watch(sessionControllerProvider);

  return GoRouter(
    initialLocation: '/',
    redirect: (context, state) {
      final path = state.uri.path;
      final isPublic = _publicRoutes.contains(path);

      if (session.isRestoring) {
        return path == '/' || path == '/recovery' ? null : '/';
      }

      if (path == '/') {
        return session.isAuthenticated ? '/dashboard' : '/welcome';
      }

      if (!session.isAuthenticated && !isPublic) {
        return '/welcome';
      }

      if (session.requiresOnboarding &&
          path != '/onboarding' &&
          path != '/recovery') {
        return '/onboarding';
      }

      if (session.isAuthenticated &&
          isPublic &&
          path != '/forgot-password' &&
          path != '/recovery') {
        return session.requiresOnboarding ? '/onboarding' : '/dashboard';
      }

      return null;
    },
    routes: [
      GoRoute(
        path: '/recovery',
        builder: (context, state) => ResetPasswordPage(
          tokenHash: state.uri.queryParameters['token_hash'],
        ),
      ),
      GoRoute(path: '/', builder: (context, state) => const SplashPage()),
      GoRoute(
        path: '/welcome',
        builder: (context, state) => const WelcomePage(),
      ),
      GoRoute(path: '/login', builder: (context, state) => const LoginPage()),
      GoRoute(
        path: '/register',
        builder: (context, state) => const RegisterPage(),
      ),
      GoRoute(
        path: '/forgot-password',
        builder: (context, state) => const ForgotPasswordPage(),
      ),
      GoRoute(
        path: '/onboarding',
        builder: (context, state) => OnboardingEditorPage(
          returnPath: state.uri.queryParameters['return'],
        ),
      ),
      GoRoute(
        path: '/dashboard',
        builder: (context, state) => const DashboardPage(),
      ),
      GoRoute(
        path: '/notifications',
        builder: (context, state) => const NotificationsPage(),
      ),
      GoRoute(path: '/meals', builder: (context, state) => const MealsPage()),
      GoRoute(
        path: '/meals/new',
        builder: (context, state) => MealEditorPage(
          initialDescription: state.uri.queryParameters['descricao'],
        ),
      ),
      GoRoute(
        path: '/meals/edit/:id',
        builder: (context, state) => MealEditorPage(
          mealId: state.pathParameters['id'],
        ),
      ),
      GoRoute(
        path: '/meals/photo',
        builder: (context, state) => const MealPhotoPickerPage(),
      ),
      GoRoute(path: '/plan', builder: (context, state) => const PlanPage()),
      GoRoute(
        path: '/progress',
        builder: (context, state) => const ProgressPage(),
      ),
      GoRoute(
        path: '/workouts',
        builder: (context, state) => const WorkoutsPage(),
      ),
      GoRoute(path: '/coach', builder: (context, state) => const CoachPage()),
      GoRoute(path: '/sync', builder: (context, state) => const SyncPage()),
      GoRoute(
        path: '/premium',
        builder: (context, state) => const PremiumPage(),
      ),
      GoRoute(
        path: '/profile',
        builder: (context, state) => const ProfileHomePage(),
      ),
    ],
  );
});

const _publicRoutes = <String>{
  '/welcome',
  '/login',
  '/register',
  '/forgot-password',
  '/recovery',
};
