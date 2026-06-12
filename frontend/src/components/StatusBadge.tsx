import type { QuestStatus } from "../types";

const styles: Record<QuestStatus, string> = {
  Open: "bg-emerald-50 text-emerald-700",
  Filling: "bg-sky-50 text-sky-700",
  Closed: "bg-amber-50 text-amber-700",
  Complete: "bg-slate-100 text-slate-600",
  Disputed: "bg-rose-50 text-rose-700",
};

export default function StatusBadge({ status }: { status: QuestStatus }) {
  return (
    <span
      className={`rounded-full px-2 py-0.5 text-xs font-medium ${styles[status]}`}
    >
      {status}
    </span>
  );
}
