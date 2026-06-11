// Shapes returned by the SideQuest API. Kept in sync with the backend DTOs.

export type QuestStatus = "Open" | "Filled" | "Completed" | "Cancelled";

export interface Category {
  id: string;
  name: string;
  slug: string;
}

export interface Poster {
  id: string;
  displayName: string;
  avatarUrl: string | null;
}

export interface Slot {
  id: string;
  status: "Open" | "Filled" | "Completed";
  assignedQuesterId: string | null;
}

export interface QuestSummary {
  id: string;
  title: string;
  description: string;
  budgetCents: number;
  currency: string;
  location: string | null;
  deadline: string | null;
  status: QuestStatus;
  slotCount: number;
  openSlotCount: number;
  category: Category;
  poster: Poster;
  bidCount: number;
  createdAt: string;
}

export interface QuestDetail extends Omit<QuestSummary, "slotCount" | "openSlotCount"> {
  slots: Slot[];
  updatedAt: string;
}

export interface CreateQuestInput {
  title: string;
  description: string;
  budgetCents: number;
  currency: string;
  location?: string | null;
  deadline?: string | null;
  categoryId: string;
  slotCount: number;
}
