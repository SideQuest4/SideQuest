import { useEffect, useState } from "react";
import { useNavigate, Link } from "react-router-dom";
import { api } from "../api";
import type { Category } from "../types";
import { dollarsToCents } from "../format";

export default function CreateQuestPage() {
  const navigate = useNavigate();
  const [categories, setCategories] = useState<Category[]>([]);
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [form, setForm] = useState({
    title: "",
    description: "",
    budget: "",
    location: "",
    categoryId: "",
    slotCount: 1,
  });

  useEffect(() => {
    api.getCategories().then((cats) => {
      setCategories(cats);
      setForm((f) => ({ ...f, categoryId: cats[0]?.id ?? "" }));
    });
  }, []);

  const update = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));

  async function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    const budgetDollars = Number(form.budget);
    if (!form.title.trim() || !form.description.trim() || !form.categoryId) {
      setError("Title, description, and category are required.");
      return;
    }
    if (!Number.isFinite(budgetDollars) || budgetDollars < 1) {
      setError("Enter a budget of at least $1.");
      return;
    }

    setSubmitting(true);
    try {
      const quest = await api.createQuest({
        title: form.title.trim(),
        description: form.description.trim(),
        budgetCents: dollarsToCents(budgetDollars),
        currency: "USD",
        location: form.location.trim() || null,
        categoryId: form.categoryId,
        slotCount: form.slotCount,
      });
      navigate(`/quests/${quest.id}`);
    } catch (err) {
      setError((err as Error).message);
      setSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-2xl">
      <Link to="/" className="text-sm text-indigo-600 hover:underline">
        ← Back to feed
      </Link>
      <h1 className="mt-2 mb-6 text-2xl font-bold">Post a quest</h1>

      <form
        onSubmit={handleSubmit}
        className="space-y-5 rounded-xl border border-slate-200 bg-white p-6 shadow-sm"
      >
        <Field label="Title">
          <input
            type="text"
            value={form.title}
            onChange={(e) => update("title", e.target.value)}
            maxLength={120}
            placeholder="e.g. Mount a TV on drywall"
            className={inputClass}
          />
        </Field>

        <Field label="Description">
          <textarea
            value={form.description}
            onChange={(e) => update("description", e.target.value)}
            maxLength={4000}
            rows={5}
            placeholder="Describe the task, what’s included, and any requirements."
            className={inputClass}
          />
        </Field>

        <div className="grid gap-5 sm:grid-cols-2">
          <Field label="Budget per slot (USD)">
            <input
              type="number"
              min={1}
              step="0.01"
              value={form.budget}
              onChange={(e) => update("budget", e.target.value)}
              placeholder="80"
              className={inputClass}
            />
          </Field>

          <Field label="Category">
            <select
              value={form.categoryId}
              onChange={(e) => update("categoryId", e.target.value)}
              className={inputClass}
            >
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
          </Field>

          <Field label="Location (optional)">
            <input
              type="text"
              value={form.location}
              onChange={(e) => update("location", e.target.value)}
              maxLength={160}
              placeholder="Seattle, WA or Remote"
              className={inputClass}
            />
          </Field>

          <Field label="Number of slots">
            <input
              type="number"
              min={1}
              max={20}
              value={form.slotCount}
              onChange={(e) =>
                update("slotCount", Math.max(1, Math.min(20, Number(e.target.value))))
              }
              className={inputClass}
            />
          </Field>
        </div>

        {error && (
          <div className="rounded-lg bg-rose-50 px-4 py-3 text-sm text-rose-700">
            {error}
          </div>
        )}

        <div className="flex items-center justify-end gap-3">
          <Link
            to="/"
            className="rounded-lg px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100"
          >
            Cancel
          </Link>
          <button
            type="submit"
            disabled={submitting}
            className="rounded-lg bg-indigo-600 px-5 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700 disabled:opacity-50"
          >
            {submitting ? "Posting…" : "Post quest"}
          </button>
        </div>
      </form>
    </div>
  );
}

const inputClass =
  "w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-sm shadow-sm outline-none focus:border-indigo-400 focus:ring-2 focus:ring-indigo-100";

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-sm font-medium text-slate-700">
        {label}
      </span>
      {children}
    </label>
  );
}
