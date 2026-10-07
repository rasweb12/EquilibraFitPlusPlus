class Workout {
  const Workout({
    required this.id,
    required this.name,
    required this.goal,
    required this.weeklyFrequency,
    required this.active,
    required this.exercises,
    required this.message,
    this.version = 1,
    this.previousWorkoutId,
    this.startDate,
    this.endDate,
    this.durationWeeks = 6,
    this.currentWeek = 1,
    this.phase = 'Fase 1',
  });

  final String id;
  final String name;
  final String goal;
  final int weeklyFrequency;
  final bool active;
  final List<WorkoutExercise> exercises;
  final String message;
  final int version;
  final String? previousWorkoutId;
  final DateTime? startDate;
  final DateTime? endDate;
  final int durationWeeks;
  final int currentWeek;
  final String phase;
}

class WorkoutExercise {
  const WorkoutExercise({
    required this.exerciseId,
    required this.name,
    required this.muscleGroup,
    required this.level,
    required this.instruction,
    required this.trainingDay,
    required this.completedToday,
    required this.order,
    required this.sets,
    required this.repetitions,
    required this.restSeconds,
    this.id,
    this.equipment,
    this.note,
    this.targetLoadKg,
    this.targetRpe,
    this.minRepetitions,
    this.maxRepetitions,
    this.progressionReason,
  });

  final String? id;
  final String exerciseId;
  final String name;
  final String muscleGroup;
  final String level;
  final String? equipment;
  final String instruction;
  final int trainingDay;
  final bool completedToday;
  final int order;
  final String? note;
  final int sets;
  final String repetitions;
  final int restSeconds;
  final double? targetLoadKg;
  final int? targetRpe;
  final int? minRepetitions;
  final int? maxRepetitions;
  final String? progressionReason;
}

class WorkoutGenerationConfig {
  const WorkoutGenerationConfig({
    required this.goal,
    required this.level,
    required this.daysPerWeek,
    required this.durationMinutes,
    required this.equipment,
    required this.limitations,
    required this.priorityMuscleGroups,
    this.durationWeeks = 6,
  });

  final String goal;
  final String level;
  final int daysPerWeek;
  final int durationMinutes;
  final int durationWeeks;
  final List<String> equipment;
  final List<String> limitations;
  final List<String> priorityMuscleGroups;
}

class WorkoutSetEntry {
  const WorkoutSetEntry({
    required this.exerciseId,
    required this.setNumber,
    required this.repetitions,
    required this.rpe,
    this.loadKg,
    this.note,
    this.pain = false,
    this.painDescription,
  });

  final String exerciseId;
  final int setNumber;
  final double? loadKg;
  final int repetitions;
  final int rpe;
  final String? note;
  final bool pain;
  final String? painDescription;
}

class WorkoutSessionResult {
  const WorkoutSessionResult({
    required this.id,
    required this.summary,
  });

  final String id;
  final String summary;
}

class WorkoutEvolutionProposal {
  const WorkoutEvolutionProposal({
    required this.id,
    required this.summary,
    required this.status,
    required this.proposedVersion,
    required this.message,
    required this.changes,
  });

  final String id;
  final String summary;
  final String status;
  final int proposedVersion;
  final String message;
  final List<WorkoutEvolutionChange> changes;
}

class WorkoutEvolutionChange {
  const WorkoutEvolutionChange({
    required this.exerciseName,
    required this.currentSets,
    required this.proposedSets,
    required this.currentRepetitions,
    required this.proposedRepetitions,
    required this.currentRestSeconds,
    required this.proposedRestSeconds,
    required this.reason,
    this.currentLoadKg,
    this.proposedLoadKg,
  });

  final String exerciseName;
  final int currentSets;
  final int proposedSets;
  final String currentRepetitions;
  final String proposedRepetitions;
  final double? currentLoadKg;
  final double? proposedLoadKg;
  final int currentRestSeconds;
  final int proposedRestSeconds;
  final String reason;
}

class WorkoutProgressionSuggestion {
  const WorkoutProgressionSuggestion({
    required this.id,
    required this.workoutExerciseId,
    required this.exerciseName,
    required this.reason,
    required this.status,
    required this.message,
    this.currentLoadKg,
    this.suggestedLoadKg,
    this.targetRpe,
    this.minRepetitions,
    this.maxRepetitions,
  });

  final String id;
  final String workoutExerciseId;
  final String exerciseName;
  final double? currentLoadKg;
  final double? suggestedLoadKg;
  final int? targetRpe;
  final int? minRepetitions;
  final int? maxRepetitions;
  final String reason;
  final String status;
  final String message;
}

class ExerciseHistory {
  const ExerciseHistory({
    required this.exerciseId,
    required this.name,
    required this.muscleGroup,
    required this.totalVolumeKg,
    required this.loadEvolutionKg,
    required this.sessions,
    this.previousLoadKg,
    this.maxLoadKg,
  });

  final String exerciseId;
  final String name;
  final String muscleGroup;
  final double? previousLoadKg;
  final double? maxLoadKg;
  final double totalVolumeKg;
  final double loadEvolutionKg;
  final List<ExerciseHistorySession> sessions;
}

class ExerciseHistorySession {
  const ExerciseHistorySession({
    required this.date,
    required this.totalRepetitions,
    required this.volumeKg,
    this.maxLoadKg,
    this.averageRpe,
  });

  final DateTime? date;
  final double? maxLoadKg;
  final int totalRepetitions;
  final double volumeKg;
  final int? averageRpe;
}
