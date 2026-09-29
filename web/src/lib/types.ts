export type DocumentStatus = "pending" | "processing" | "ready" | "failed";

export type SourceType = "text" | "markdown" | "pdf";

export interface DocumentResponse {
  id: string;
  title: string;
  sourceType: SourceType;
  status: DocumentStatus;
  createdAt: string;
  chunkCount: number;
}

export interface ConversationResponse {
  id: string;
  title: string | null;
  createdAt: string;
  messageCount: number;
  lastMessageAt: string | null;
}

export interface CitationResponse {
  chunkId: string;
  documentTitle: string;
  snippet: string;
  score: number;
}

export interface MessageResponse {
  id: string;
  role: "user" | "assistant";
  content: string;
  createdAt: string;
  citations: CitationResponse[];
}

export interface ChatMessage {
  id: string;
  role: "user" | "assistant";
  content: string;
  createdAt: string;
  citations: CitationResponse[];
  streaming?: boolean;
  error?: boolean;
}
