import { createClient } from "@/lib/supabase/client";
import type {
  ConversationResponse,
  DocumentResponse,
  MessageResponse,
} from "@/lib/types";

export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5028";

export class ApiError extends Error {
  constructor(
    message: string,
    public readonly status: number
  ) {
    super(message);
  }
}

async function authHeaders(): Promise<Record<string, string>> {
  const supabase = createClient();
  const {
    data: { session },
  } = await supabase.auth.getSession();
  return session ? { Authorization: `Bearer ${session.access_token}` } : {};
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: { ...(await authHeaders()), ...(init.headers ?? {}) },
  });

  if (!response.ok) {
    throw new ApiError(await problemTitle(response), response.status);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

async function problemTitle(response: Response): Promise<string> {
  try {
    const problem = await response.json();
    return problem?.title ?? `Request failed (${response.status})`;
  } catch {
    return `Request failed (${response.status})`;
  }
}

export const api = {
  listDocuments: () => request<DocumentResponse[]>("/api/documents"),

  getDocument: (id: string) => request<DocumentResponse>(`/api/documents/${id}`),

  uploadDocumentText: (payload: { title: string; content: string; sourceType: "text" | "markdown" }) =>
    request<DocumentResponse>("/api/documents/text", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    }),

  uploadDocumentFile: (file: File) => {
    const form = new FormData();
    form.append("file", file);
    return request<DocumentResponse>("/api/documents", { method: "POST", body: form });
  },

  deleteDocument: (id: string) =>
    request<void>(`/api/documents/${id}`, { method: "DELETE" }),

  listConversations: () => request<ConversationResponse[]>("/api/conversations"),

  createConversation: (title?: string) =>
    request<ConversationResponse>("/api/conversations", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title: title ?? null }),
    }),

  renameConversation: (id: string, title: string) =>
    request<ConversationResponse>(`/api/conversations/${id}`, {
      method: "PATCH",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ title }),
    }),

  deleteConversation: (id: string) =>
    request<void>(`/api/conversations/${id}`, { method: "DELETE" }),

  generateConversationTitle: (id: string) =>
    request<{ id: string; title: string | null }>(`/api/conversations/${id}/title`, {
      method: "POST",
    }),

  getMessages: (id: string) =>
    request<MessageResponse[]>(`/api/conversations/${id}/messages`),
};
