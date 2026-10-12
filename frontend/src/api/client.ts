const API_BASE_URL =
  (import.meta as { env?: Record<string, string | undefined> }).env?.VITE_API_BASE_URL ??
  'http://localhost:5083'

function parseApiError(response: Response, rawBody: string): string {
  const trimmed = rawBody.trim()

  if (!trimmed) {
    return `Request failed (${response.status})`
  }

  let parseFailed = false

  try {
    const parsed = JSON.parse(trimmed) as { error?: string; title?: string }
    if (parsed.error?.trim()) {
      return parsed.error.trim()
    }

    if (parsed.title?.trim()) {
      return parsed.title.trim()
    }
  } catch {
    parseFailed = true
  }

  if (
    parseFailed &&
    (trimmed.startsWith('<!DOCTYPE html') || trimmed.startsWith('<html') || trimmed.includes('System.'))
  ) {
    return `Request failed (${response.status})`
  }

  return trimmed.length > 180 ? `${trimmed.slice(0, 180)}…` : trimmed
}

export async function apiPost<TResponse>(
  path: string,
  body: unknown,
  signal?: AbortSignal,
): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(body),
    signal,
  })

  if (!response.ok) {
    const text = await response.text()
    throw new Error(parseApiError(response, text))
  }

  return (await response.json()) as TResponse
}

export async function apiGet<TResponse>(path: string, signal?: AbortSignal): Promise<TResponse> {
  const response = await fetch(`${API_BASE_URL}${path}`, { signal })
  if (!response.ok) {
    const text = await response.text()
    throw new Error(parseApiError(response, text))
  }

  return (await response.json()) as TResponse
}
