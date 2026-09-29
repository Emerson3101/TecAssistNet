import { API_URL, ApiError } from "@/lib/api";
import { createClient } from "@/lib/supabase/client";
import type { CitationResponse } from "@/lib/types";

export interface ChatStreamHandlers {
  onToken: (token: string) => void;
  onCitation: (citation: CitationResponse) => void;
  onDone: (ids: { userMessageId: string; assistantMessageId: string }) => void;
}

export async function streamChatAnswer(
  conversationId: string,
  content: string,
  handlers: ChatStreamHandlers,
  signal?: AbortSignal
): Promise<void> {
  const supabase = createClient();
  const {
    data: { session },
  } = await supabase.auth.getSession();

  const response = await fetch(
    `${API_URL}/api/conversations/${conversationId}/messages`,
    {
      method: "POST",
      signal,
      headers: {
        "Content-Type": "application/json",
        ...(session ? { Authorization: `Bearer ${session.access_token}` } : {}),
      },
      body: JSON.stringify({ content }),
    }
  );

  if (!response.ok) {
    throw new ApiError(await problemTitle(response), response.status);
  }

  const reader = response.body!.getReader();
  const decoder = new TextDecoder();
  let buffer = "";

  while (true) {
    const { done, value } = await reader.read();
    if (done) {
      break;
    }

    buffer += decoder.decode(value, { stream: true });

    let separatorIndex: number;
    while ((separatorIndex = buffer.indexOf("\n\n")) !== -1) {
      const frame = buffer.slice(0, separatorIndex);
      buffer = buffer.slice(separatorIndex + 2);
      const completed = handleFrame(frame, handlers);
      if (completed) {
        return;
      }
    }
  }
}

function handleFrame(frame: string, handlers: ChatStreamHandlers): boolean {
  let eventName: string | null = null;
  let dataLine: string | null = null;

  for (const line of frame.split("\n")) {
    if (line.startsWith("event: ")) {
      eventName = line.slice(7).trim();
    } else if (line.startsWith("data: ")) {
      dataLine = line.slice(6);
    }
  }

  if (!eventName || !dataLine) {
    return false;
  }

  const data = JSON.parse(dataLine);

  switch (eventName) {
    case "token":
      handlers.onToken(data.text as string);
      return false;
    case "citation":
      handlers.onCitation(data as CitationResponse);
      return false;
    case "done":
      handlers.onDone(data);
      return true;
    case "error":
      throw new ApiError(
        data.message ?? "The AI service failed to generate a response.",
        502
      );
    default:
      return false;
  }
}

async function problemTitle(response: Response): Promise<string> {
  try {
    const problem = await response.json();
    return problem?.title ?? `Request failed (${response.status})`;
  } catch {
    return `Request failed (${response.status})`;
  }
}
