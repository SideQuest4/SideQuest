import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import type { Bid } from "../types";
import { formatMoney, formatRelativeTime } from "../format";

interface Props {
  questId: string;
  currency: string;
  acceptingBids: boolean;
  /** Multi-slot quests are fixed-price: questers claim a slot, no bidding/counter. */
  multiSlot: boolean;
  /** The fixed price per slot (used for multi-slot claims). */
  fixedPriceCents: number;
  /** Changes whenever a live bid event arrives, prompting a bid-list refresh. */
  reloadKey?: number;
  /** Called after any action that can change the quest (e.g. a bid is accepted). */
  onQuestChanged: () => void;
}

export default function BidPanel({
  questId,
  currency,
  acceptingBids,
  multiSlot,
  fixedPriceCents,
  reloadKey,
  onQuestChanged,
}: Props) {
  const noun = multiSlot ? "claim" : "bid";
  const [bids, setBids] = useState<Bid[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const loadBids = useCallback(() => {
    setLoading(true);
    api
      .getBids(questId)
      .then(setBids)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [questId]);

  // Reload on mount, on quest change, and whenever a live bid event bumps reloadKey.
  useEffect(loadBids, [loadBids, reloadKey]);

  // Run an action, then refresh both the bid list and the parent quest.
  async function run(id: string, action: () => Promise<unknown>) {
    setBusyId(id);
    setError(null);
    try {
      await action();
      loadBids();
      onQuestChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="mt-6 space-y-6">
      <RoleNote />

      {acceptingBids ? (
        multiSlot ? (
          <ClaimSlotForm
            currency={currency}
            fixedPriceCents={fixedPriceCents}
            onClaim={() =>
              run("new", async () => {
                await api.submitBid(questId, fixedPriceCents);
              })
            }
          />
        ) : (
          <PlaceBidForm
            currency={currency}
            onSubmit={(cents, msg) =>
              run("new", async () => {
                await api.submitBid(questId, cents, msg);
              })
            }
          />
        )
      ) : (
        <div className="rounded-xl border border-slate-200 bg-white p-4 text-center text-sm text-slate-500">
          This quest is no longer accepting {multiSlot ? "claims" : "bids"}.
        </div>
      )}

      <div>
        <h2 className="mb-3 text-sm font-semibold text-slate-700">
          {multiSlot ? "Claims" : "Bids"} {bids.length > 0 && `(${bids.length})`}
        </h2>

        {error && (
          <div className="mb-3 rounded-lg bg-rose-50 px-4 py-2 text-sm text-rose-700">
            {error}
          </div>
        )}

        {loading ? (
          <div className="h-20 animate-pulse rounded-xl bg-white" />
        ) : bids.length === 0 ? (
          <div className="rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500">
            No {noun}s yet. Be the first to {noun} a slot.
          </div>
        ) : (
          <ul className="space-y-3">
            {bids.map((bid) => (
              <BidRow
                key={bid.id}
                bid={bid}
                currency={currency}
                busy={busyId === bid.id}
                acceptingBids={acceptingBids}
                multiSlot={multiSlot}
                onAccept={() => run(bid.id, () => api.acceptBid(bid.id))}
                onDecline={() => run(bid.id, () => api.declineBid(bid.id))}
                onCounter={(cents) =>
                  run(bid.id, () => api.counterBid(bid.id, cents))
                }
                onRespond={(accept) =>
                  run(bid.id, () => api.respondToCounter(bid.id, accept))
                }
              />
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}

function RoleNote() {
  return (
    <p className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
      Roles aren’t separated yet (no login). Both the quester’s bid form and the
      poster’s review controls are shown so you can test the full loop.
    </p>
  );
}

function ClaimSlotForm({
  currency,
  fixedPriceCents,
  onClaim,
}: {
  currency: string;
  fixedPriceCents: number;
  onClaim: () => void;
}) {
  return (
    <div className="flex flex-col items-start justify-between gap-3 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:flex-row sm:items-center">
      <div>
        <h2 className="text-sm font-semibold text-slate-700">Claim a slot</h2>
        <p className="text-sm text-slate-500">
          Fixed price — every quester is paid the same{" "}
          <span className="font-semibold text-slate-700">
            {formatMoney(fixedPriceCents, currency)}
          </span>{" "}
          for this quest.
        </p>
      </div>
      <button
        onClick={onClaim}
        className="rounded-lg bg-indigo-600 px-5 py-2 text-sm font-semibold text-white transition hover:bg-indigo-700"
      >
        Claim for {formatMoney(fixedPriceCents, currency)}
      </button>
    </div>
  );
}

function PlaceBidForm({
  currency,
  onSubmit,
}: {
  currency: string;
  onSubmit: (cents: number, message?: string) => void;
}) {
  const [amount, setAmount] = useState("");
  const [message, setMessage] = useState("");

  function submit(e: React.FormEvent) {
    e.preventDefault();
    const dollars = Number(amount);
    if (!Number.isFinite(dollars) || dollars < 1) return;
    onSubmit(Math.round(dollars * 100), message.trim() || undefined);
    setAmount("");
    setMessage("");
  }

  return (
    <form
      onSubmit={submit}
      className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm"
    >
      <h2 className="mb-3 text-sm font-semibold text-slate-700">Place a bid</h2>
      <div className="flex flex-col gap-3 sm:flex-row">
        <div className="relative sm:w-40">
          <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-slate-400">
            $
          </span>
          <input
            type="number"
            min={1}
            step="0.01"
            value={amount}
            onChange={(e) => setAmount(e.target.value)}
            placeholder="Your price"
            className="w-full rounded-lg border border-slate-200 py-2 pl-7 pr-3 text-sm outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
          />
        </div>
        <input
          type="text"
          value={message}
          onChange={(e) => setMessage(e.target.value)}
          placeholder="Optional message"
          maxLength={1000}
          className="flex-1 rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
        />
        <button
          type="submit"
          className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-indigo-700"
        >
          Bid {currency}
        </button>
      </div>
    </form>
  );
}

function BidRow({
  bid,
  currency,
  busy,
  acceptingBids,
  multiSlot,
  onAccept,
  onDecline,
  onCounter,
  onRespond,
}: {
  bid: Bid;
  currency: string;
  busy: boolean;
  acceptingBids: boolean;
  multiSlot: boolean;
  onAccept: () => void;
  onDecline: () => void;
  onCounter: (cents: number) => void;
  onRespond: (accept: boolean) => void;
}) {
  const [countering, setCountering] = useState(false);
  const [counterAmount, setCounterAmount] = useState("");

  function submitCounter() {
    const dollars = Number(counterAmount);
    if (!Number.isFinite(dollars) || dollars < 1) return;
    onCounter(Math.round(dollars * 100));
    setCountering(false);
    setCounterAmount("");
  }

  return (
    <li className="rounded-xl border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex items-start justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <span className="font-medium text-slate-800">
              {bid.quester.displayName}
            </span>
            <BidStatusTag bid={bid} />
          </div>
          {bid.message && (
            <p className="mt-1 text-sm text-slate-600">{bid.message}</p>
          )}
          <p className="mt-1 text-xs text-slate-400">
            {formatRelativeTime(bid.createdAt)}
          </p>
        </div>
        <div className="shrink-0 text-right">
          <div className="text-lg font-bold text-slate-900">
            {formatMoney(bid.effectiveAmountCents, currency)}
          </div>
          {bid.counterAmountCents != null && (
            <div className="text-xs text-slate-400 line-through">
              {formatMoney(bid.amountCents, currency)}
            </div>
          )}
        </div>
      </div>

      {/* Actions depend on bid status. Disabled once the quest stops taking bids. */}
      {acceptingBids && (bid.status === "Pending" || bid.status === "Countered") && (
        <div className="mt-3 border-t border-slate-100 pt-3">
          {bid.status === "Pending" && !countering && (
            <div className="flex flex-wrap gap-2">
              <ActionButton kind="primary" disabled={busy} onClick={onAccept}>
                {multiSlot ? "Approve" : "Accept"}
              </ActionButton>
              {/* Counter-offers are single-slot only; multi-slot is fixed-price. */}
              {!multiSlot && (
                <ActionButton kind="ghost" disabled={busy} onClick={() => setCountering(true)}>
                  Counter
                </ActionButton>
              )}
              <ActionButton kind="danger" disabled={busy} onClick={onDecline}>
                Decline
              </ActionButton>
            </div>
          )}

          {bid.status === "Pending" && countering && (
            <div className="flex flex-wrap items-center gap-2">
              <div className="relative w-32">
                <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-sm text-slate-400">
                  $
                </span>
                <input
                  type="number"
                  min={1}
                  step="0.01"
                  value={counterAmount}
                  onChange={(e) => setCounterAmount(e.target.value)}
                  placeholder="Counter"
                  autoFocus
                  className="w-full rounded-lg border border-slate-200 py-1.5 pl-7 pr-2 text-sm outline-none focus:border-indigo-400"
                />
              </div>
              <ActionButton kind="primary" disabled={busy} onClick={submitCounter}>
                Send counter
              </ActionButton>
              <ActionButton kind="ghost" disabled={busy} onClick={() => setCountering(false)}>
                Cancel
              </ActionButton>
            </div>
          )}

          {bid.status === "Countered" && (
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-xs text-slate-500">
                Quester’s response to the counter:
              </span>
              <ActionButton kind="primary" disabled={busy} onClick={() => onRespond(true)}>
                Accept counter
              </ActionButton>
              <ActionButton kind="danger" disabled={busy} onClick={() => onRespond(false)}>
                Decline
              </ActionButton>
            </div>
          )}
        </div>
      )}
    </li>
  );
}

function BidStatusTag({ bid }: { bid: Bid }) {
  const map: Record<Bid["status"], string> = {
    Pending: "bg-slate-100 text-slate-600",
    Countered: "bg-sky-50 text-sky-700",
    Accepted: "bg-emerald-50 text-emerald-700",
    Declined: "bg-rose-50 text-rose-600",
  };
  return (
    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${map[bid.status]}`}>
      {bid.status}
    </span>
  );
}

function ActionButton({
  kind,
  disabled,
  onClick,
  children,
}: {
  kind: "primary" | "ghost" | "danger";
  disabled: boolean;
  onClick: () => void;
  children: React.ReactNode;
}) {
  const styles = {
    primary: "bg-indigo-600 text-white hover:bg-indigo-700",
    ghost: "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50",
    danger: "bg-white text-rose-600 ring-1 ring-rose-200 hover:bg-rose-50",
  }[kind];
  return (
    <button
      onClick={onClick}
      disabled={disabled}
      className={`rounded-lg px-3 py-1.5 text-sm font-medium transition disabled:opacity-50 ${styles}`}
    >
      {children}
    </button>
  );
}
