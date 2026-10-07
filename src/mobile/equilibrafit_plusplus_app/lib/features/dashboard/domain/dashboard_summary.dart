class DashboardSummary {
  const DashboardSummary({
    required this.date,
    required this.supportMessage,
    required this.food,
    required this.workout,
    required this.progress,
    this.profile,
    this.currentPlan,
  });

  final DateTime date;
  final String supportMessage;
  final DashboardProfile? profile;
  final DashboardFood food;
  final DashboardWorkout workout;
  final DashboardProgress progress;
  final DashboardPlan? currentPlan;
}

class DashboardProfile {
  const DashboardProfile({
    required this.currentWeightKg,
    required this.heightCm,
    required this.bmi,
    required this.goal,
  });

  final double currentWeightKg;
  final double heightCm;
  final double bmi;
  final String goal;
}

class DashboardFood {
  const DashboardFood({
    required this.calories,
    required this.proteinG,
    required this.carbsG,
    required this.fatG,
    required this.meals,
    this.calorieGoal,
    this.calorieBalance,
  });

  final double calories;
  final double proteinG;
  final double carbsG;
  final double fatG;
  final int meals;
  final double? calorieGoal;
  final double? calorieBalance;
}

class DashboardWorkout {
  const DashboardWorkout({
    required this.activeWorkouts,
    required this.weeklyFrequency,
    this.nextWorkout,
  });

  final int activeWorkouts;
  final int weeklyFrequency;
  final String? nextWorkout;
}

class DashboardProgress {
  const DashboardProgress({
    this.lastWeightKg,
    this.lastUpdate,
    this.weightVariationKg,
  });

  final double? lastWeightKg;
  final DateTime? lastUpdate;
  final double? weightVariationKg;
}

class DashboardPlan {
  const DashboardPlan({
    required this.id,
    required this.dailyCalories,
    required this.goal,
    required this.explanation,
    required this.mealSuggestions,
  });

  final String id;
  final int dailyCalories;
  final String goal;
  final String explanation;
  final List<DashboardMealSuggestion> mealSuggestions;
}

class DashboardMealSuggestion {
  const DashboardMealSuggestion({
    required this.name,
    required this.description,
    required this.calories,
  });

  final String name;
  final String description;
  final int calories;
}
