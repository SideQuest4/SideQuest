import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api";
import type { QuestDetail } from "../types";
import { formatMoney, formatRelativeTime } from "../format";
import StatusBadge from "../components/StatusBadge";

export default function QuestDetailPage() {
  const { id } = useParams<{ id: string }>();
  const [quest, setQuest] = useState<QuestDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!id) return;
    setLoading(true);
    api
      .getQuest(id)
      .then(setQuest)
      .catch((e: Error) => setError(e.message))
      .finally(() => setLoading(false));
  }, [id]);

  if (loading) {
    return <div className="h-64 animate-pulse rounded-xl bg-white" />;
  }
  if (error || !quest) {
    return (
      <div className="rounded-xl border border-dashed border-slate-300 bg-white p-12 text-center">
        <p className="text-lg font-semibold text-slate-700">Quest not found</p>
        <p className="mt-1 text-sm text-slate-500">{error}</p>
        <Link to="/" className="mt-4 inline-block text-sm text-indigo-600 hover:underline">
          ← Back to feed
        </Link>
      </div>
    );
  }

  const filled = quest.slots.filter((s) => s.status !== "Open").length;

  return (
    <div className="mx-auto max-w-3xl">
      <Link to="/" className="text-sm text-indigo-600 hover:underline">
        ← Back to feed
      </Link>

      <div className="mt-3 rounded-xl border border-slate-200 bg-white p-6 shadow-sm">
        <div className="flex items-start justify-between gap-4">
          <div>
            <div className="mb-2 flex items-center gap-2">
              <span className="rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">
                {quest.category.name}
              </span>
              <StatusBadge status={quest.status} />
            </div>
            <h1 className="text-2xl font-bold">{quest.title}</h1>
            <p className="mt-1 text-sm text-slate-500">
              Posted by {quest.poster.displayName} ·{" "}
              {formatRelativeTime(quest.createdAt)}
            </p>
          </div>
          <div className="shrink-0 text-right">
            <div className="text-2xl font-bold">
              {formatMoney(quest.budgetCents, quest.currency)}
            </div>
            <div className="text-xs text-slate-400">per slot</div>
          </div>
        </div>

        <p className="mt-5 whitespace-pre-wrap text-sm leading-relaxed text-slate-700">
          {quest.description}
        </p>

        <dl className="mt-6 grid grid-cols-2 gap-4 border-t border-slate-100 pt-5 text-sm sm:grid-cols-3">
          <Detail label="Location" value={quest.location ?? "—"} />
          <Detail
            label="Slots"
            value={`${filled}/${quest.slots.length} filled`}
          />
          <Detail label="Bids" value={String(quest.bidCount)} />
        </dl>
      </div>

      {/* Slots */}
      <div className="mt-6">
        <h2 className="mb-3 text-sm font-semibold text-slate-700">Slots</h2>
        <div className="grid gap-2 sm:grid-cols-2">
          {quest.slots.map((slot, i) => (
            <div
              key={slot.id}
              className="flex items-center justify-between rounded-lg border border-slate-200 bg-white px-4 py-3 text-sm"
            >
              <span className="font-medium text-slate-700">Slot {i + 1}</span>
              <span
                className={
                  slot.status === "Open"
                    ? "text-emerald-600"
                    : "text-slate-400"
                }
              >
                {slot.status}
              </span>
            </div>
          ))}
        </div>
      </div>

      {/* Bidding is the next milestone (Week 5). */}
      <div className="mt-6 rounded-xl border border-dashed border-slate-300 bg-white p-6 text-center text-sm text-slate-500">
        Bidding &amp; counter-offers arrive in the next milestone.
      </div>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div>
      <dt className="text-xs uppercase tracking-wide text-slate-400">{label}</dt>
      <dd className="mt-0.5 font-medium text-slate-700">{value}</dd>
    </div>
  );
}
