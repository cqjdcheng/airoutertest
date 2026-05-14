export type SelfTestHistoryItem = {
  id: string;
  testRecordId?: number | null;
  siteUrl: string;
  siteName?: string | null;
  siteSlug?: string | null;
  modelName: string;
  modelSlug?: string | null;
  status: string;
  riskScore: number;
  riskLevel: string;
  resultSummary: string;
  firstTokenMs?: number | null;
  fullResponseMs?: number | null;
  testedAt: string;
};

const STORAGE_KEY = "cheapai:self-test-history";
const UPDATE_EVENT = "cheapai:self-test-history-updated";
const MAX_ITEMS = 50;

export function readSelfTestHistory(): SelfTestHistoryItem[] {
  if (typeof window === "undefined") {
    return [];
  }

  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return [];
    }

    const parsed = JSON.parse(raw) as SelfTestHistoryItem[];
    return Array.isArray(parsed) ? parsed.sort((left, right) => Date.parse(right.testedAt) - Date.parse(left.testedAt)) : [];
  } catch {
    return [];
  }
}

export function saveSelfTestHistoryItem(item: SelfTestHistoryItem) {
  if (typeof window === "undefined") {
    return;
  }

  const nextItems = [item, ...readSelfTestHistory().filter((entry) => entry.id !== item.id)].slice(0, MAX_ITEMS);
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(nextItems));
  window.dispatchEvent(new Event(UPDATE_EVENT));
}

export function subscribeSelfTestHistory(listener: () => void) {
  if (typeof window === "undefined") {
    return () => undefined;
  }

  const handleUpdate = () => listener();
  const handleStorage = (event: StorageEvent) => {
    if (event.key === STORAGE_KEY) {
      listener();
    }
  };

  window.addEventListener(UPDATE_EVENT, handleUpdate);
  window.addEventListener("storage", handleStorage);

  return () => {
    window.removeEventListener(UPDATE_EVENT, handleUpdate);
    window.removeEventListener("storage", handleStorage);
  };
}
