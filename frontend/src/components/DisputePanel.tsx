import { useState } from "react";
import { api } from "../api";
import type { DisputeOutcome, QuestDetail } from "../types";
import { formatMoney, formatRelativeTime } from "../format";

/**
 * Dispute controls for a quest with escrow at stake.
 * - In progress (held escrow): a participant can report a problem and freeze it.
 * - Disputed: a manual-review resolution refunds the poster or pays the questers.
 *
 * Auth is stubbed, so opening a dispute is attributed to the poster and the
 * resolution buttons stand in for an admin/moderator.
 */
export default function DisputePanel({
  quest,
  onChanged,
}: {
  quest: QuestDetail;
  onChanged: () => void;
}) {
  const isDisputed = quest.status === "Disputed";
  const canDispute =
    !isDisputed &&
    quest.escrow.heldCount > 0 &&
    (quest.status === "Filling" || quest.status === "Closed");

  if (isDisputed) return <DisputedState quest={quest} onChanged={onChanged} />;
  if (canDispute) return <ReportProblem quest={quest} onChanged={onChanged} />;
  return null;
}

function ReportProblem({
  quest,
  onChanged,
}: {
  quest: QuestDetail;
  onChanged: () => void;
}) {
  const [open, setOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function submit() {
    if (reason.trim().length < 3) {
      setError("Please describe the problem.");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      // Attributed to the poster while auth is stubbed.
      await api.openDispute(quest.id, quest.poster.id, reason.trim());
      onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="mt-6 rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      {!open ? (
        <button
          onClick={() => setOpen(true)}
          className="text-sm font-medium text-rose-600 hover:underline"
        >
          ⚠ Report a problem with this quest
        </button>
      ) : (
        <div>
          <h2 className="text-sm font-semibold text-slate-700">Report a problem</h2>
          <p className="mt-1 text-xs text-slate-500">
            This freezes the quest and its {formatMoney(quest.escrow.heldAmountCents, quest.currency)} escrow
            for manual review.
          </p>
          <textarea
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            maxLength={1000}
            rows={3}
            placeholder="What went wrong?"
            className="mt-3 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm outline-none focus:border-rose-400"
          />
          {error && <p className="mt-1 text-xs text-rose-600">{error}</p>}
          <div className="mt-2 flex justify-end gap-2">
            <button
              onClick={() => {
                setOpen(false);
                setReason("");
                setError(null);
              }}
              className="rounded-lg px-3 py-1.5 text-xs font-medium text-slate-500 hover:bg-slate-100"
            >
              Cancel
            </button>
            <button
              onClick={submit}
              disabled={saving}
              className="rounded-lg bg-rose-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-rose-700 disabled:opacity-50"
            >
              {saving ? "Submitting…" : "Open dispute"}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function DisputedState({
  quest,
  onChanged,
}: {
  quest: QuestDetail;
  onChanged: () => void;
}) {
  const [resolving, setResolving] = useState<DisputeOutcome | null>(null);
  const [error, setError] = useState<string | null>(null);

  async function resolve(outcome: DisputeOutcome) {
    setResolving(outcome);
    setError(null);
    try {
      await api.resolveDispute(quest.id, outcome);
      onChanged();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setResolving(null);
    }
  }

  return (
    <div className="mt-6 rounded-xl border border-amber-300 bg-amber-50 p-5 shadow-sm">
      <div className="flex items-center gap-2">
        <span className="rounded-full bg-amber-200 px-2 py-0.5 text-xs font-semibold text-amber-800">
          Under dispute
        </span>
        {quest.disputedAt && (
          <span className="text-xs text-amber-700">
            {formatRelativeTime(quest.disputedAt)}
          </span>
        )}
      </div>

      {quest.disputeReason && (
        <p className="mt-3 text-sm text-slate-700">
          <span className="font-medium">Reason:</span> {quest.disputeReason}
        </p>
      )}

      <p className="mt-3 text-xs text-slate-600">
        Manual review — resolve by returning the{" "}
        {formatMoney(quest.escrow.heldAmountCents, quest.currency)} in escrow to the
        poster, or releasing it to the quester(s).
      </p>

      {error && (
        <div className="mt-3 rounded-lg bg-rose-50 px-4 py-2 text-sm text-rose-700">
          {error}
        </div>
      )}

      <div className="mt-4 flex flex-wrap gap-2">
        <button
          onClick={() => resolve("refund")}
          disabled={resolving !== null}
          className="rounded-lg bg-slate-700 px-4 py-2 text-sm font-semibold text-white transition hover:bg-slate-800 disabled:opacity-50"
        >
          {resolving === "refund" ? "Refunding…" : "Refund poster"}
        </button>
        <button
          onClick={() => resolve("release")}
          disabled={resolving !== null}
          className="rounded-lg bg-emerald-600 px-4 py-2 text-sm font-semibold text-white transition hover:bg-emerald-700 disabled:opacity-50"
        >
          {resolving === "release" ? "Releasing…" : "Release to quester"}
        </button>
      </div>

      <p className="mt-3 text-xs text-amber-700">
        Admin action (login not wired yet). V1 disputes are resolved manually.
      </p>
    </div>
  );
}
