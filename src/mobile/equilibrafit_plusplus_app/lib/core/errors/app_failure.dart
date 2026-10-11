class AppFailure implements Exception {
  const AppFailure(this.message, {this.code, this.apiCode});

  final String message;
  final String? code;
  final String? apiCode;

  @override
  String toString() => message;
}
