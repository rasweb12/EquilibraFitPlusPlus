class OnboardingAnswer {
  const OnboardingAnswer({
    required this.birthDate,
    required this.biologicalSex,
    required this.heightCm,
    required this.currentWeightKg,
    required this.goal,
    required this.activityLevel,
    required this.trainingDaysPerWeek,
    required this.preferences,
    required this.restrictions,
    required this.notes,
  });

  final DateTime birthDate;
  final String biologicalSex;
  final double heightCm;
  final double currentWeightKg;
  final String goal;
  final String activityLevel;
  final int trainingDaysPerWeek;
  final List<String> preferences;
  final List<String> restrictions;
  final List<String> notes;

  Map<String, Object?> toJson() {
    return <String, Object?>{
      'dataNascimento': _dateOnly(birthDate),
      'sexoBiologico': biologicalSex,
      'alturaCm': heightCm,
      'pesoAtualKg': currentWeightKg,
      'objetivo': goal,
      'nivelAtividade': activityLevel,
      'diasTreinoSemana': trainingDaysPerWeek,
      'preferencias': preferences,
      'restricoes': restrictions,
      'observacoes': notes,
    };
  }

  String _dateOnly(DateTime value) {
    final year = value.year.toString().padLeft(4, '0');
    final month = value.month.toString().padLeft(2, '0');
    final day = value.day.toString().padLeft(2, '0');
    return '$year-$month-$day';
  }
}
