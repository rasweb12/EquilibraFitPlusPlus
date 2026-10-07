enum SyncStatus {
  pending('Pending'),
  syncing('Syncing'),
  synced('Synced'),
  failed('Failed'),
  conflict('Conflict');

  const SyncStatus(this.value);

  final String value;
}
