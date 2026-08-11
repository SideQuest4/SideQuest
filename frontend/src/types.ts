// Shapes returned by the SideQuest API. Kept in sync with the backend DTOs.

export type QuestStatus =
  | "Open"
  | "Filling"
  | "Closed"
  | "Complete"
  | "Disputed";

export type SlotStatus =
  | "Open"
  | "Active"
  | "Completed"
  | "Dropped"
  | "Kicked"
  | "Disputed";

export interface Category {
  id: string;
  name: string;
  slug: string;
}

export interface Poster {
  id: string;
  displayName: string;
  avatarUrl: string | null;
  averageStars: number | null;
  ratingCount: number;
}

export interface Slot {
  id: string;
  status: SlotStatus;
  assignedQuesterId: string | null;
  assignedQuesterName: string | null;
  disputeReason: string | null;
  disputedAt: string | null;
  checkedInAt: string | null;
  posterConfirmedAt: string | null;
  noShowReportedAt: string | null;
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

export interface EscrowSummary {
  heldCount: number;
  releasedCount: number;
  heldAmountCents: number;
  releasedAmountCents: number;
}

export interface QuestDetail extends Omit<QuestSummary, "slotCount" | "openSlotCount"> {
  slots: Slot[];
  escrow: EscrowSummary;
  updatedAt: string;
}

export type DisputeOutcome = "refund" | "release";
export type SlotRefundOutcome = "reopen" | "cancel";

export type BidStatus = "Pending" | "Countered" | "Accepted" | "Declined";

export interface Quester {
  id: string;
  displayName: string;
  avatarUrl: string | null;
}

export interface Bid {
  id: string;
  questId: string;
  quester: Quester;
  amountCents: number;
  counterAmountCents: number | null;
  effectiveAmountCents: number;
  message: string | null;
  status: BidStatus;
  createdAt: string;
  updatedAt: string;
}

export interface Rating {
  id: string;
  questId: string;
  raterId: string;
  raterName: string;
  rateeId: string;
  rateeName: string;
  stars: number;
  comment: string | null;
  createdAt: string;
}

export interface UserRatingSummary {
  userId: string;
  averageStars: number | null;
  ratingCount: number;
  recent: Rating[];
}

export interface CreateRatingInput {
  raterId: string;
  rateeId: string;
  stars: number;
  comment?: string | null;
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
