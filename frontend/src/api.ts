import type {
  Bid,
  Category,
  CreateQuestInput,
  QuestDetail,
  QuestSummary,
} from "./types";

// Requests go to /api, which Vite proxies to the backend in dev.
const BASE = "/api";

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${BASE}${path}`, {
    headers: { "Content-Type": "application/json", ...init?.headers },
    ...init,
  });

  if (!res.ok) {
    let detail = `${res.status} ${res.statusText}`;
    try {
      const body = await res.text();
      if (body) detail = body;
    } catch {
      /* ignore body parse errors */
    }
    throw new Error(detail);
  }

  // 204 No Content
  if (res.status === 204) return undefined as T;
  return (await res.json()) as T;
}

export interface FeedParams {
  category?: string;
  search?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}

export const api = {
  getCategories: () => request<Category[]>("/categories"),

  getFeed: (params: FeedParams = {}) => {
    const qs = new URLSearchParams();
    if (params.category) qs.set("category", params.category);
    if (params.search) qs.set("search", params.search);
    if (params.status) qs.set("status", params.status);
    if (params.page) qs.set("page", String(params.page));
    if (params.pageSize) qs.set("pageSize", String(params.pageSize));
    const q = qs.toString();
    return request<QuestSummary[]>(`/quests${q ? `?${q}` : ""}`);
  },

  getQuest: (id: string) => request<QuestDetail>(`/quests/${id}`),

  createQuest: (input: CreateQuestInput) =>
    request<QuestDetail>("/quests", {
      method: "POST",
      body: JSON.stringify(input),
    }),

  completeQuest: (id: string) =>
    request<QuestDetail>(`/quests/${id}/complete`, { method: "POST" }),

  // ---- Bidding ----
  getBids: (questId: string) => request<Bid[]>(`/quests/${questId}/bids`),

  submitBid: (questId: string, amountCents: number, message?: string) =>
    request<Bid>(`/quests/${questId}/bids`, {
      method: "POST",
      body: JSON.stringify({ amountCents, message: message || null }),
    }),

  counterBid: (bidId: string, counterAmountCents: number) =>
    request<Bid>(`/bids/${bidId}/counter`, {
      method: "POST",
      body: JSON.stringify({ counterAmountCents }),
    }),

  acceptBid: (bidId: string) =>
    request<Bid>(`/bids/${bidId}/accept`, { method: "POST" }),

  declineBid: (bidId: string) =>
    request<Bid>(`/bids/${bidId}/decline`, { method: "POST" }),

  respondToCounter: (bidId: string, accept: boolean) =>
    request<Bid>(`/bids/${bidId}/respond`, {
      method: "POST",
      body: JSON.stringify({ accept }),
    }),
};
