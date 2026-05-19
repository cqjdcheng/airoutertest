const API_BASE_URL = process.env.UMI_APP_API_BASE_URL || "";
const ACCESS_TOKEN_KEY = "cheapai_admin_access_token";

export type AdminProfile = {
  id: number;
  username: string;
  displayName: string;
};

type ApiEnvelope<T> = {
  code: number;
  message: string;
  data: T;
  requestId: string;
  timestamp: string;
};

export function getAccessToken() {
  if (typeof window === "undefined") {
    return "";
  }

  return window.localStorage.getItem(ACCESS_TOKEN_KEY) ?? "";
}

export function setAccessToken(token: string) {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.setItem(ACCESS_TOKEN_KEY, token);
}

export function clearAccessToken() {
  if (typeof window === "undefined") {
    return;
  }

  window.localStorage.removeItem(ACCESS_TOKEN_KEY);
}

async function rawRequest<T>(path: string, init?: RequestInit, retryOnUnauthorized = true): Promise<T> {
  const headers = new Headers(init?.headers ?? {});
  if (!(init?.body instanceof FormData)) {
    headers.set("Content-Type", "application/json");
  }

  const token = getAccessToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
    credentials: "include"
  });

  const text = await response.text();
  let json: ApiEnvelope<T> | null = null;

  if (text) {
    try {
      json = JSON.parse(text) as ApiEnvelope<T>;
    } catch {
      json = null;
    }
  }

  if (response.status === 401 && retryOnUnauthorized && await refreshAccessToken()) {
    return rawRequest<T>(path, init, false);
  }

  if (!response.ok || !json || json.code !== 0) {
    throw new Error(json?.message || `接口请求失败：HTTP ${response.status}`);
  }

  return json.data;
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  return rawRequest<T>(path, init, true);
}

export async function refreshAccessToken() {
  try {
    const result = await rawRequest<{
      accessToken: string;
      expiresIn: number;
      admin: AdminProfile;
    }>("/api/v1/auth/refresh", { method: "POST", body: "{}" }, false);
    setAccessToken(result.accessToken);
    return true;
  } catch {
    clearAccessToken();
    return false;
  }
}

export async function login(username: string, password: string) {
  return rawRequest<{
    accessToken: string;
    expiresIn: number;
    admin: AdminProfile;
  }>("/api/v1/auth/login", {
    method: "POST",
    body: JSON.stringify({ username, password })
  }, false);
}

export async function fetchCurrentAdmin() {
  return apiRequest<AdminProfile>("/api/v1/auth/me");
}

export async function logout() {
  try {
    await rawRequest<void>("/api/v1/auth/logout", { method: "POST", body: "{}" }, false);
  } finally {
    clearAccessToken();
  }
}
