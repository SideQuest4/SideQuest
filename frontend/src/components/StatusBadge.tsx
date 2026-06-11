import type { QuestStatus } from "../types";

const styles: Record<QuestStatus, string> = {
  Open: "bg-emerald-50 text-emerald-700",
  Filled: "bg-amber-50 text-amber-700",
  Completed: "bg-slate-100 text-slate-600",
  Cancelled: "bg-rose-50 text-rose-700",
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
