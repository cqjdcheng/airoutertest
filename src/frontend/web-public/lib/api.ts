const serverApiBaseUrl =
  process.env.API_BASE_URL ??
  process.env.NEXT_PUBLIC_API_BASE_URL ??
  "http://127.0.0.1:5157";

const browserApiBaseUrl =
  process.env.NEXT_PUBLIC_API_BASE_URL || "";

export const apiBaseUrl = typeof window === "undefined" ? serverApiBaseUrl : browserApiBaseUrl;

export async function getJson<T>(path: string): Promise<T | null> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      cache: "no-store"
    });

    if (!response.ok) {
      return null;
    }

    return (await response.json()) as T;
  } catch {
    return null;
  }
}

export async function postJson<T>(path: string, payload: unknown): Promise<T | null> {
  try {
    const response = await fetch(`${apiBaseUrl}${path}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json"
      },
      body: JSON.stringify(payload)
    });

    if (!response.ok) {
      return null;
    }

    return (await response.json()) as T;
  } catch {
    return null;
  }
}

export type PublicEnvelope<T> = {
  code: number;
  message: string;
  data: T;
  requestId: string;
  timestamp: string;
};
