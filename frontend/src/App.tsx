import { Link, Outlet, useLocation } from "react-router-dom";

export default function App() {
  const { pathname } = useLocation();
  const onCreate = pathname === "/quests/new";

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white/80 backdrop-blur">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <Link to="/" className="flex items-center gap-2 text-lg font-bold">
            <span className="text-2xl">🗺️</span>
            <span>
              Side<span className="text-indigo-600">Quest</span>
            </span>
          </Link>
          {!onCreate && (
            <Link
              to="/quests/new"
              className="rounded-lg bg-indigo-600 px-4 py-2 text-sm font-semibold text-white shadow-sm transition hover:bg-indigo-700"
            >
              + Post a quest
            </Link>
          )}
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 py-6">
        <Outlet />
      </main>

      <footer className="mx-auto max-w-5xl px-4 py-10 text-center text-xs text-slate-400">
        SideQuest V1 · open quest marketplace
      </footer>
    </div>
  );
}
