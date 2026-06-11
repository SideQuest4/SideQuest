import { useEffect, useState } from "react";
import { api } from "../api";
import type { Category, QuestSummary } from "../types";
import QuestCard from "../components/QuestCard";

export default function FeedPage() {
  const [quests, setQuests] = useState<QuestSummary[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [activeCategory, setActiveCategory] = useState<string>("");
  const [search, setSearch] = useState("");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Load categories once.
  useEffect(() => {
    api.getCategories().then(setCategories).catch(() => {
      /* non-fatal: filter chips just won't render */
    });
  }, []);

  // Reload the feed whenever the category or (debounced) search changes.
  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    const handle = setTimeout(() => {
      api
        .getFeed({ category: activeCategory || undefined, search: search || undefined })
        .then((data) => {
          if (!cancelled) setQuests(data);
        })
        .catch((e: Error) => {
          if (!cancelled) setError(e.message);
        })
        .finally(() => {
          if (!cancelled) setLoading(false);
        });
    }, 250);

    return () => {
      cancelled = true;
      clearTimeout(handle);
    };
  }, [activeCategory, search]);

  return (
    <div>
      <div className="mb-6">
        <h1 className="text-2xl font-bold">Quest feed</h1>
        <p className="text-sm text-slate-500">
          Browse open quests and place a bid.
        </p>
      </div>

      {/* Search */}
      <input
        type="search"
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        placeholder="Search quests…"
        className="mb-4 w-full rounded-lg border border-slate-200 bg-white px-4 py-2.5 text-sm shadow-sm outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100"
      />

      {/* Category chips */}
      {categories.length > 0 && (
        <div className="mb-6 flex flex-wrap gap-2">
          <Chip
            label="All"
            active={activeCategory === ""}
            onClick={() => setActiveCategory("")}
          />
          {categories.map((c) => (
            <Chip
              key={c.id}
              label={c.name}
              active={activeCategory === c.slug}
              onClick={() => setActiveCategory(c.slug)}
            />
          ))}
        </div>
      )}

      {/* Results */}
      {loading ? (
        <SkeletonList />
      ) : error ? (
        <EmptyState
          title="Couldn’t load the feed"
          subtitle={error}
        />
      ) : quests.length === 0 ? (
        <EmptyState
          title="No quests yet"
          subtitle="Try a different category or be the first to post one."
        />
      ) : (
        <div className="grid gap-4">
          {quests.map((q) => (
            <QuestCard key={q.id} quest={q} />
          ))}
        </div>
      )}
    </div>
  );
}

function Chip({
  label,
  active,
  onClick,
}: {
  label: string;
  active: boolean;
  onClick: () => void;
}) {
  return (
    <button
      onClick={onClick}
      className={`rounded-full px-3 py-1.5 text-sm font-medium transition ${
        active
          ? "bg-indigo-600 text-white"
          : "bg-white text-slate-600 ring-1 ring-slate-200 hover:bg-slate-50"
      }`}
    >
      {label}
    </button>
  );
}

function SkeletonList() {
  return (
    <div className="grid gap-4">
      {Array.from({ length: 3 }).map((_, i) => (
        <div
          key={i}
          className="h-32 animate-pulse rounded-xl border border-slate-200 bg-white"
        />
      ))}
    </div>
  );
}

function EmptyState({ title, subtitle }: { title: string; subtitle: string }) {
  return (
    <div className="rounded-xl border border-dashed border-slate-300 bg-white p-12 text-center">
      <p className="text-lg font-semibold text-slate-700">{title}</p>
      <p className="mt-1 text-sm text-slate-500">{subtitle}</p>
    </div>
  );
}
