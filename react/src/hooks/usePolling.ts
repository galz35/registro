import { useState, useEffect, useCallback } from 'react';

export function usePolling<T>(fetcher: () => Promise<T>, interval = 30000) {
  const [data, setData] = useState<T | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetch = useCallback(async () => {
    try {
      setLoading(true);
      const result = await fetcher();
      setData(result);
      setError(null);
    } catch (err: any) {
      setError(err?.response?.data?.message || err.message);
    } finally {
      setLoading(false);
    }
  }, [fetcher]);

  useEffect(() => {
    fetch();
    const timer = setInterval(fetch, interval);
    return () => clearInterval(timer);
  }, [fetch, interval]);

  return { data, loading, error, refetch: fetch };
}
