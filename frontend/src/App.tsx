import { Link, Outlet, useLocation } from "react-router-dom";

export default function App() {
  const { pathname } = useLocation();
  const onCreate = pathname === "/quests/new";

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="sticky top-0 z-10 border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-2.5">
          <Link to="/" className="flex items-center gap-2.5" aria-label="SideQuest home">
            {/* Transparent-background logo blends into the bar — no box, no crop. */}
            <img src="/logo.png" alt="" className="h-9 w-9 object-contain" />
            <span className="flex flex-col leading-none">
              <span className="text-lg font-bold tracking-tight text-slate-900">
                SideQuest
              </span>
              <span className="mt-0.5 text-xs font-medium text-slate-500">
                Begin your next quest
              </span>
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
