class MealEntry {
  const MealEntry({
    required this.id,
    required this.dateTime,
    required this.type,
    required this.origin,
    required this.calories,
    required this.proteinG,
    required this.carbsG,
    required this.fatG,
    required this.confirmedByUser,
    required this.items,
    required this.message,
  });

  final String id;
  final DateTime dateTime;
  final String type;
  final String origin;
  final double calories;
  final double proteinG;
  final double carbsG;
  final double fatG;
  final bool confirmedByUser;
  final List<MealItem> items;
  final String message;
}

class MealItem {
  const MealItem({
    required this.name,
    required this.quantity,
    required this.unit,
    required this.calories,
    required this.proteinG,
    required this.carbsG,
    required this.fatG,
    this.id,
    this.source,
  });

  final String? id;
  final String name;
  final double quantity;
  final String unit;
  final double calories;
  final double proteinG;
  final double carbsG;
  final double fatG;
  final String? source;

  Map<String, Object?> toRequestJson() {
    return <String, Object?>{
      'nome': name,
      'quantidade': quantity,
      'unidade': unit,
      'calorias': calories,
      'proteinaG': proteinG,
      'carboidratoG': carbsG,
      'gorduraG': fatG,
      'fonteNutricional': source,
    };
  }
}

class MealEstimation {
  const MealEstimation({
    required this.items,
    required this.message,
    required this.model,
    required this.fallbackUsed,
  });

  final List<MealItem> items;
  final String message;
  final String model;
  final bool fallbackUsed;
}
