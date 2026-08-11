import { useState } from "react";
import { api } from "../api";
import type { DisputeOutcome, QuestDetail, Slot, SlotRefundOutcome } from "../types";
import { formatMoney, formatRelativeTime } from "../format";

const statusColor: Record<string, string> = {
  Open: "text-emerald-600",
  Active: "text-sky-600",
  Completed: "text-slate-500",
  Dropped: "text-slate-400",
  Kicked: "text-slate-400",
  Disputed: "text-rose-600",
};

/**
 * One slot with its contextual actions. While auth is stubbed, both the
 * quester's action (check-in) and the poster/admin actions (confirm, no-show,
 * dispute, resolve) are shown together.
 */
export default function SlotCard({
  slot,
  index,
  quest,
  onChanged,
}: {
  slot: Slot;
  index: number;
  quest: QuestDetail;
  onChanged: () => void;
}) {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [showDispute, setShowDispute] = useState(false);
  const [reason, setReason] = useState("");

  async function run(fn: () => Promise<unknown>) {
    setBusy(true);
    setError(null);
    try {
      await fn();
      onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setBusy(false);
    }
  }

  const isActive = slot.status === "Active";
  const isDisputed = slot.status === "Disputed";

  return (
    <div className="rounded-lg border border-slate-200 bg-white px-4 py-3 text-sm">
      <div className="flex items-center justify-between gap-3">
        <span className="font-medium text-slate-700">
          Slot {index + 1}
          {slot.assignedQuesterName && (
            <span className="ml-2 font-normal text-slate-500">
              · {slot.assignedQuesterName}
            </span>
          )}
        </span>
        <span className={`font-medium ${statusColor[slot.status] ?? "text-slate-500"}`}>
          {slot.status}
        </span>
      </div>

      {/* Check-in / no-show state for an active slot */}
      {isActive && (
        <div className="mt-2 space-y-2">
          <div className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-slate-500">
            <span>
              {slot.checkedInAt
                ? `✓ Checked in ${formatRelativeTime(slot.checkedInAt)}`
                : "Not checked in"}
            </span>
            {slot.posterConfirmedAt && <span>· ✓ Poster confirmed</span>}
          </div>

          <div className="flex flex-wrap gap-2">
            {!slot.checkedInAt && slot.assignedQuesterId && (
              <button
                onClick={() => run(() => api.checkInSlot(slot.id, slot.assignedQuesterId!))}
                disabled={busy}
                className="rounded-md bg-sky-600 px-3 py-1 text-xs font-semibold text-white hover:bg-sky-700 disabled:opacity-50"
              >
                Check in (quester)
              </button>
            )}
            {slot.checkedInAt && !slot.posterConfirmedAt && (
              <button
                onClick={() => run(() => api.confirmSlot(slot.id, quest.poster.id))}
                disabled={busy}
                className="rounded-md bg-emerald-600 px-3 py-1 text-xs font-semibold text-white hover:bg-emerald-700 disabled:opacity-50"
              >
                Confirm showed (poster)
              </button>
            )}
            <button
              onClick={() => run(() => api.reportNoShow(slot.id, quest.poster.id))}
              disabled={busy}
              className="rounded-md border border-rose-300 px-3 py-1 text-xs font-semibold text-rose-600 hover:bg-rose-50 disabled:opacity-50"
            >
              Report no-show
            </button>
            <button
              onClick={() => setShowDispute((v) => !v)}
              disabled={busy}
              className="rounded-md border border-slate-300 px-3 py-1 text-xs font-medium text-slate-600 hover:bg-slate-50 disabled:opacity-50"
            >
              Report a problem
            </button>
          </div>

          {showDispute && (
            <div className="rounded-md border border-slate-200 p-2">
              <textarea
                value={reason}
                onChange={(e) => setReason(e.target.value)}
                maxLength={1000}
                rows={2}
                placeholder="What went wrong with this slot?"
                className="w-full rounded border border-slate-200 px-2 py-1 text-sm outline-none focus:border-rose-400"
              />
              <div className="mt-1 flex justify-end gap-2">
                <button
                  onClick={() => {
                    setShowDispute(false);
                    setReason("");
                  }}
                  className="rounded px-2 py-1 text-xs text-slate-500 hover:bg-slate-100"
                >
                  Cancel
                </button>
                <button
                  onClick={() =>
                    run(async () => {
                      if (reason.trim().length < 3) throw new Error("Describe the problem.");
                      await api.disputeSlot(slot.id, quest.poster.id, reason.trim());
                    })
                  }
                  disabled={busy}
                  className="rounded bg-rose-600 px-3 py-1 text-xs font-semibold text-white hover:bg-rose-700 disabled:opacity-50"
                >
                  Open dispute
                </button>
              </div>
            </div>
          )}
        </div>
      )}

      {/* Disputed: manual-review resolution */}
      {isDisputed && (
        <div className="mt-2 rounded-md border border-amber-300 bg-amber-50 p-3">
          {slot.disputeReason && (
            <p className="text-sm text-slate-700">
              <span className="font-medium">Dispute:</span> {slot.disputeReason}
            </p>
          )}
          {slot.noShowReportedAt && (
            <p className="mt-1 text-xs text-rose-600">Flagged as a no-show.</p>
          )}
          <p className="mt-2 text-xs text-slate-600">
            Resolve the {formatMoney(quest.budgetCents, quest.currency)} in escrow:
          </p>
          <div className="mt-2 flex flex-wrap gap-2">
            <ResolveButton label="Release to quester" busy={busy}
              onClick={() => run(() => api.resolveSlotDispute(slot.id, "release"))} />
            <ResolveButton label="Refund → reopen slot" busy={busy}
              onClick={() => run(() => resolveRefund(slot.id, "reopen"))} />
            <ResolveButton label="Refund → cancel slot" busy={busy}
              onClick={() => run(() => resolveRefund(slot.id, "cancel"))} />
          </div>
          <p className="mt-2 text-xs text-amber-700">
            Admin action (login not wired). Refund choice is the poster's; a release auto-closes the slot.
          </p>
        </div>
      )}

      {error && <p className="mt-2 text-xs text-rose-600">{error}</p>}
    </div>
  );

  function resolveRefund(slotId: string, slotOutcome: SlotRefundOutcome) {
    const outcome: DisputeOutcome = "refund";
    return api.resolveSlotDispute(slotId, outcome, slotOutcome);
  }
}

function ResolveButton({
  label,
  busy,
  onClick,
}: {
  label: string;
  busy: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      disabled={busy}
      className="rounded-md bg-slate-700 px-3 py-1.5 text-xs font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
    >
      {label}
    </button>
  );
}
