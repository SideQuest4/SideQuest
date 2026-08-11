/** Read-only star rating display with an optional average + count label. */
export default function Stars({
  value,
  count,
  showEmpty = true,
}: {
  value: number | null;
  count?: number;
  showEmpty?: boolean;
}) {
  if (value == null) {
    return showEmpty ? (
      <span className="text-xs text-slate-400">No ratings yet</span>
    ) : null;
  }

  const rounded = Math.round(value);
  return (
    <span className="inline-flex items-center gap-1" aria-label={`${value} out of 5 stars`}>
      <span className="text-sm leading-none">
        {[1, 2, 3, 4, 5].map((n) => (
          <span key={n} className={n <= rounded ? "text-amber-500" : "text-slate-300"}>
            ★
          </span>
        ))}
      </span>
      <span className="text-xs font-medium text-slate-600">
        {value.toFixed(1)}
        {count != null ? ` (${count})` : ""}
      </span>
    </span>
  );
}
