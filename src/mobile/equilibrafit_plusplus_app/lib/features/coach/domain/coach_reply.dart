class CoachReply {
  const CoachReply({
    required this.sessionId,
    required this.content,
    required this.healthNotice,
    this.fallbackUsed = false,
  });

  final String sessionId;
  final String content;
  final String healthNotice;
  final bool fallbackUsed;
}
