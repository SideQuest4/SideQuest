import { Link } from "react-router-dom";
import type { QuestSummary } from "../types";
import { formatMoney, formatRelativeTime } from "../format";
import StatusBadge from "./StatusBadge";

export default function QuestCard({ quest }: { quest: QuestSummary }) {
  const filled = quest.slotCount - quest.openSlotCount;

  return (
    <Link
      to={`/quests/${quest.id}`}
      className="block rounded-xl border border-slate-200 bg-white p-5 shadow-sm transition hover:border-indigo-300 hover:shadow-md"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <div className="mb-1 flex items-center gap-2">
            <span className="rounded-full bg-indigo-50 px-2 py-0.5 text-xs font-medium text-indigo-700">
              {quest.category.name}
            </span>
            <StatusBadge status={quest.status} />
          </div>
          <h3 className="truncate text-base font-semibold text-slate-900">
            {quest.title}
          </h3>
        </div>
        <div className="shrink-0 text-right">
          <div className="text-lg font-bold text-slate-900">
            {formatMoney(quest.budgetCents, quest.currency)}
          </div>
          <div className="text-xs text-slate-400">per slot</div>
        </div>
      </div>

      <p className="mt-2 line-clamp-2 text-sm text-slate-600">
        {quest.description}
      </p>

      <div className="mt-4 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-slate-500">
        {quest.location && <span>📍 {quest.location}</span>}
        <span>
          🎯 {filled}/{quest.slotCount} slot{quest.slotCount > 1 ? "s" : ""} filled
        </span>
        <span>💬 {quest.bidCount} bid{quest.bidCount === 1 ? "" : "s"}</span>
        <span className="ml-auto">{formatRelativeTime(quest.createdAt)}</span>
      </div>
    </Link>
  );
}
