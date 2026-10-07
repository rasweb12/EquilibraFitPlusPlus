class AppNotification {
  const AppNotification({
    required this.id,
    required this.userId,
    required this.title,
    required this.message,
    required this.status,
    required this.createdAt,
  });

  final String id;
  final String userId;
  final String title;
  final String message;
  final String status;
  final DateTime createdAt;

  bool get isRead => status.toLowerCase() == 'lida';
}
