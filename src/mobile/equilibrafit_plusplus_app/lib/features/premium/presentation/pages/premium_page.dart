import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../../../../design_system/widgets/ef_scaffold.dart';
import '../../data/google_play_billing.dart';

class PremiumPage extends ConsumerWidget {
  const PremiumPage({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final state = ref.watch(googlePlayBillingProvider);
    final billing = ref.read(googlePlayBillingProvider.notifier);
    return EfScaffold(
        title: 'EquilibraFit++ Premium',
        currentIndex: 4,
        body: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
          Text(state.premium ? 'Premium ativo' : 'Plano FREE',
              style: Theme.of(context).textTheme.titleLarge,),
          if (state.loading) const LinearProgressIndicator(),
          if (state.message != null)
            Padding(
                padding: const EdgeInsets.symmetric(vertical: 16),
                child: Text(state.message!),),
          for (final product in state.products)
            ListTile(
              title: Text(product.title),
              subtitle: Text(product.description),
              trailing: TextButton(
                  onPressed:
                      state.available ? () => billing.buy(product) : null,
                  child: Text(product.price),),
            ),
          OutlinedButton.icon(
              onPressed: state.loading ? null : billing.restore,
              icon: const Icon(Icons.restore),
              label: const Text('Restaurar compras'),),
          IconButton(
              onPressed: state.loading ? null : billing.load,
              icon: const Icon(Icons.refresh),
              tooltip: 'Atualizar assinatura',),
        ],),);
  }
}
