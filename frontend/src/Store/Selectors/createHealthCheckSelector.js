import { createSelector } from 'reselect';
import getDocsUrl from 'Utilities/getDocsUrl';

function createHealthCheckSelector() {
  return createSelector(
    (state) => state.system.health,
    (state) => state.app,
    (health, app) => {
      const items = [...health.items];

      if (!app.isConnected) {
        items.push({
          source: 'UI',
          type: 'warning',
          message: 'Could not connect to SignalR, UI won\'t update',
          wikiUrl: getDocsUrl('health-checks', 'could-not-connect-to-signalr')
        });
      }

      return items;
    }
  );
}

export default createHealthCheckSelector;
