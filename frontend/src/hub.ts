import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  LogLevel,
} from "@microsoft/signalr";
import type { QuestSummary } from "./types";

// A single shared connection to the live quest board, started lazily and reused
// across the app. Components subscribe via the helpers below and unsubscribe on
// cleanup; group membership for a quest is reference-counted so overlapping
// watchers don't tear each other's subscription down.

let connection: HubConnection | null = null;
let starting: Promise<void> | null = null;
const groupCounts = new Map<string, number>();

function getConnection(): HubConnection {
  if (!connection) {
    connection = new HubConnectionBuilder()
      .withUrl("/hubs/quests")
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();
  }
  return connection;
}

async function ensureStarted(): Promise<HubConnection> {
  const conn = getConnection();
  if (conn.state === HubConnectionState.Connected) return conn;
  if (!starting) {
    starting = conn.start().finally(() => {
      starting = null;
    });
  }
  await starting;
  return conn;
}

/** Listen for any quest summary change (new quest, slot fill, status change). */
export function onQuestChanged(
  handler: (quest: QuestSummary) => void
): () => void {
  const conn = getConnection();
  conn.on("QuestChanged", handler);
  ensureStarted().catch(() => {
    /* reconnect logic will retry */
  });
  return () => conn.off("QuestChanged", handler);
}

/** Watch a single quest's bid activity; fires whenever its bids change. */
export function watchQuestBids(questId: string, handler: () => void): () => void {
  const conn = getConnection();

  const listener = (changedQuestId: string) => {
    if (changedQuestId === questId) handler();
  };
  conn.on("BidsChanged", listener);

  // Join the quest's group (ref-counted across overlapping watchers).
  const next = (groupCounts.get(questId) ?? 0) + 1;
  groupCounts.set(questId, next);
  ensureStarted()
    .then((started) => {
      if (next === 1) return started.invoke("JoinQuest", questId);
    })
    .catch(() => {});

  return () => {
    conn.off("BidsChanged", listener);
    const remaining = (groupCounts.get(questId) ?? 1) - 1;
    if (remaining <= 0) {
      groupCounts.delete(questId);
      if (conn.state === HubConnectionState.Connected) {
        conn.invoke("LeaveQuest", questId).catch(() => {});
      }
    } else {
      groupCounts.set(questId, remaining);
    }
  };
}
