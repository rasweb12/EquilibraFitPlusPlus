class DailyHabits {
  const DailyHabits({
    int waterCups = 0,
    int? waterMl,
    this.waterGoalMl = 2700,
    this.sleepHours = 0,
    this.sleepGoalHours = 8,
    this.mood = 3,
    this.meditationDone = false,
    this.stretchingDone = false,
  }) : waterMl = waterMl ?? waterCups * cupVolumeMl;

  static const int cupVolumeMl = 250;

  final int waterMl;
  final int waterGoalMl;
  final double sleepHours;
  final double sleepGoalHours;
  final int mood;
  final bool meditationDone;
  final bool stretchingDone;

  int get waterCups => _cupsFromMilliliters(waterMl);

  bool get waterDone => waterMl >= waterGoalMl;
  bool get sleepDone => sleepHours >= sleepGoalHours;

  double get waterProgress {
    if (waterGoalMl <= 0) {
      return 0;
    }

    return (waterMl / waterGoalMl).clamp(0, 1).toDouble();
  }

  DailyHabits copyWith({
    int? waterCups,
    int? waterMl,
    int? waterGoalMl,
    double? sleepHours,
    double? sleepGoalHours,
    int? mood,
    bool? meditationDone,
    bool? stretchingDone,
  }) {
    return DailyHabits(
      waterMl: waterMl ??
          (waterCups == null ? this.waterMl : waterCups * cupVolumeMl),
      waterGoalMl: waterGoalMl ?? this.waterGoalMl,
      sleepHours: sleepHours ?? this.sleepHours,
      sleepGoalHours: sleepGoalHours ?? this.sleepGoalHours,
      mood: mood ?? this.mood,
      meditationDone: meditationDone ?? this.meditationDone,
      stretchingDone: stretchingDone ?? this.stretchingDone,
    );
  }

  Map<String, Object?> toJson() {
    return <String, Object?>{
      'waterCups': waterCups,
      'waterMl': waterMl,
      'waterGoalMl': waterGoalMl,
      'sleepHours': sleepHours,
      'sleepGoalHours': sleepGoalHours,
      'mood': mood,
      'meditationDone': meditationDone,
      'stretchingDone': stretchingDone,
    };
  }

  Map<String, Object?> toApiJson({required String date}) {
    return <String, Object?>{
      'data': date,
      'aguaMl': waterMl,
      'metaAguaMl': waterGoalMl,
      'sonoHoras': sleepHours,
      'metaSonoHoras': sleepGoalHours,
      'humor': mood,
      'meditacaoRealizada': meditationDone,
      'alongamentoRealizado': stretchingDone,
    };
  }

  factory DailyHabits.fromJson(Map<dynamic, dynamic> json) {
    final moodValue = (json['mood'] as num?)?.toInt() ?? 3;
    final waterMl =
        (json['waterMl'] as num?)?.toInt() ?? (json['aguaMl'] as num?)?.toInt();

    return DailyHabits(
      waterMl:
          waterMl ?? ((json['waterCups'] as num?)?.toInt() ?? 0) * cupVolumeMl,
      waterGoalMl: (json['waterGoalMl'] as num?)?.toInt() ??
          (json['metaAguaMl'] as num?)?.toInt() ??
          2700,
      sleepHours: (json['sleepHours'] as num?)?.toDouble() ?? 0,
      sleepGoalHours: (json['sleepGoalHours'] as num?)?.toDouble() ??
          (json['metaSonoHoras'] as num?)?.toDouble() ??
          8,
      mood: moodValue.clamp(1, 5).toInt(),
      meditationDone: json['meditationDone'] == true,
      stretchingDone: json['stretchingDone'] == true,
    );
  }

  factory DailyHabits.fromApiJson(Map<String, Object?> json) {
    final moodValue = _intValue(json, 'humor', 'Humor') ?? 3;

    return DailyHabits(
      waterCups: _cupsFromMilliliters(
        _intValue(json, 'aguaMl', 'AguaMl') ?? 0,
      ),
      waterMl: _intValue(json, 'aguaMl', 'AguaMl') ?? 0,
      waterGoalMl: _intValue(json, 'metaAguaMl', 'MetaAguaMl') ?? 2700,
      sleepHours: _doubleValue(json, 'sonoHoras', 'SonoHoras') ?? 0,
      sleepGoalHours: _doubleValue(json, 'metaSonoHoras', 'MetaSonoHoras') ?? 8,
      mood: moodValue.clamp(1, 5).toInt(),
      meditationDone:
          _boolValue(json, 'meditacaoRealizada', 'MeditacaoRealizada'),
      stretchingDone:
          _boolValue(json, 'alongamentoRealizado', 'AlongamentoRealizado'),
    );
  }

  static int _cupsFromMilliliters(int value) {
    if (value <= 0) {
      return 0;
    }

    return (value / cupVolumeMl).round().clamp(0, 40).toInt();
  }

  static int? _intValue(
    Map<String, Object?> json,
    String key,
    String pascalKey,
  ) {
    final value = json[key] ?? json[pascalKey];
    return value is num ? value.toInt() : null;
  }

  static double? _doubleValue(
    Map<String, Object?> json,
    String key,
    String pascalKey,
  ) {
    final value = json[key] ?? json[pascalKey];
    return value is num ? value.toDouble() : null;
  }

  static bool _boolValue(
    Map<String, Object?> json,
    String key,
    String pascalKey,
  ) {
    final value = json[key] ?? json[pascalKey];
    return value == true;
  }
}
