import { history } from "@umijs/max";
import { PropsWithChildren, useEffect, useState } from "react";
import { getAccessToken } from "@/services/api";

export default function AuthGuard({ children }: PropsWithChildren) {
  const [allowed, setAllowed] = useState(() => Boolean(getAccessToken()));

  useEffect(() => {
    const token = getAccessToken();
    setAllowed(Boolean(token));

    if (!token) {
      history.replace("/login");
    }
  }, []);

  if (!allowed) {
    return null;
  }

  return <>{children}</>;
}
