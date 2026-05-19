import { apiRequest } from "./api";

export function uploadFile(file: File) {
  const formData = new FormData();
  formData.append("file", file);
  return apiRequest<{ url: string }>("/api/v1/admin/uploads", {
    method: "POST",
    body: formData
  });
}
