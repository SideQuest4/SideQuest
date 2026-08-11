import { useCallback, useEffect, useState } from "react";
import { api } from "../api";
import type { QuestDetail, Rating } from "../types";
import Stars from "./Stars";

interface Pair {
  raterId: string;
  raterName: string;
  rateeId: string;
  rateeName: string;
}

/**
 * Ratings for a completed quest. Each poster<->quester relationship gets one
 * rating per direction. Auth isn't wired, so both directions are shown here;
 * once login exists this collapses to just "your" outgoing rating.
 */
export default function RatingPanel({
  quest,
  reloadKey,
}: {
  quest: QuestDetail;
  reloadKey?: number;
}) {
  const [ratings, setRatings] = useState<Rating[] | null>(null);

  const load = useCallback(() => {
    api
      .getQuestRatings(quest.id)
      .then(setRatings)
      .catch(() => setRatings([]));
  }, [quest.id]);

  useEffect(() => {
    load();
  }, [load, reloadKey]);

  if (quest.status !== "Complete") return null;

  // Distinct questers who filled a slot.
  const questers = Array.from(
    new Map(
      quest.slots
        .filter((s) => s.assignedQuesterId)
        .map((s) => [s.assignedQuesterId!, s.assignedQuesterName ?? "Quester"] as const),
    ).entries(),
  ).map(([id, name]) => ({ id, name }));

  const poster = quest.poster;
  const pairs: Pair[] = [];
  for (const q of questers) {
    pairs.push({
      raterId: poster.id,
      raterName: poster.displayName,
      rateeId: q.id,
      rateeName: q.name,
    });
    pairs.push({
      raterId: q.id,
      raterName: q.name,
      rateeId: poster.id,
      rateeName: poster.displayName,
    });
  }

  return (
    <div className="mt-6 rounded-xl border border-slate-200 bg-white p-5 shadow-sm">
      <h2 className="text-sm font-semibold text-slate-700">Ratings</h2>
      <p className="mt-1 text-xs text-slate-500">
        Leave a rating for each person you worked with — one per direction.
      </p>

      <div className="mt-4 space-y-3">
        {pairs.map((p) => {
          const existing = ratings?.find(
            (r) => r.raterId === p.raterId && r.rateeId === p.rateeId,
          );
          return (
            <RatingRow
              key={`${p.raterId}->${p.rateeId}`}
              questId={quest.id}
              pair={p}
              existing={existing}
              onSubmitted={load}
            />
          );
        })}
      </div>

      <p className="mt-3 text-xs text-amber-700">
        Both directions shown while login is stubbed. With auth, you'd only rate
        your counterpart.
      </p>
    </div>
  );
}

function RatingRow({
  questId,
  pair,
  existing,
  onSubmitted,
}: {
  questId: string;
  pair: Pair;
  existing?: Rating;
  onSubmitted: () => void;
}) {
  const [stars, setStars] = useState(0);
  const [hover, setHover] = useState(0);
  const [comment, setComment] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const label = (
    <span className="text-sm text-slate-600">
      <span className="font-medium text-slate-800">{pair.raterName}</span> →{" "}
      {pair.rateeName}
    </span>
  );

  if (existing) {
    return (
      <div className="rounded-lg border border-slate-100 bg-slate-50 px-4 py-3">
        <div className="flex items-center justify-between gap-3">
          {label}
          <Stars value={existing.stars} />
        </div>
        {existing.comment && (
          <p className="mt-1 text-sm text-slate-600">“{existing.comment}”</p>
        )}
      </div>
    );
  }

  async function submit() {
    if (stars < 1) {
      setError("Pick a star rating first.");
      return;
    }
    setSaving(true);
    setError(null);
    try {
      await api.createRating(questId, {
        raterId: pair.raterId,
        rateeId: pair.rateeId,
        stars,
        comment: comment.trim() || null,
      });
      onSubmitted();
    } catch (e) {
      setError((e as Error).message);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="rounded-lg border border-slate-200 px-4 py-3">
      <div className="flex items-center justify-between gap-3">
        {label}
        <div className="flex" onMouseLeave={() => setHover(0)}>
          {[1, 2, 3, 4, 5].map((n) => (
            <button
              key={n}
              type="button"
              onClick={() => setStars(n)}
              onMouseEnter={() => setHover(n)}
              className="px-0.5 text-xl leading-none"
              aria-label={`${n} star${n > 1 ? "s" : ""}`}
            >
              <span
                className={
                  (hover || stars) >= n ? "text-amber-500" : "text-slate-300"
                }
              >
                ★
              </span>
            </button>
          ))}
        </div>
      </div>

      <input
        value={comment}
        onChange={(e) => setComment(e.target.value)}
        maxLength={1000}
        placeholder="Add a comment (optional)"
        className="mt-2 w-full rounded-lg border border-slate-200 px-3 py-1.5 text-sm outline-none focus:border-indigo-400"
      />

      {error && <p className="mt-1 text-xs text-rose-600">{error}</p>}

      <div className="mt-2 flex justify-end">
        <button
          onClick={submit}
          disabled={saving}
          className="rounded-lg bg-indigo-600 px-3 py-1.5 text-xs font-semibold text-white transition hover:bg-indigo-700 disabled:opacity-50"
        >
          {saving ? "Saving…" : "Submit rating"}
        </button>
      </div>
    </div>
  );
}
