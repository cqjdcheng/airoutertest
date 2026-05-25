import Link from "next/link";
import { getJson, type PublicEnvelope } from "@/lib/api";
import { money, score, trustScore } from "@/lib/format";

type ModelItem = {
  modelSlug: string;
  modelName: string;
  requestName: string;
};

type RankingItem = {
  siteSlug: string;
  siteName: string;
  effectiveInputPriceUsd?: number;
  effectiveOutputPriceUsd?: number;
  riskScore?: number;
  riskLevel: string;
};

type PagedResult<T> = {
  items: T[];
  total: number;
};

type RankingResponse = {
  model: {
    slug: string;
    displayName: string;
  };
  result: PagedResult<RankingItem>;
};

export async function RankingPageView({ selectedModelSlug }: { selectedModelSlug?: string }) {
  const modelsResponse = await getJson<PublicEnvelope<PagedResult<ModelItem>>>("/api/v1/public/models?page=1&pageSize=100");
  const models = modelsResponse?.data.items ?? [];
  const selectedModel = selectedModelSlug && models.some((model) => model.modelSlug === selectedModelSlug)
    ? selectedModelSlug
    : models[0]?.modelSlug;
  const rankingResponse = selectedModel
    ? await getJson<PublicEnvelope<RankingResponse>>(
        `/api/v1/public/rankings/models/${encodeURIComponent(selectedModel)}?rankingType=price&window=7d&page=1&pageSize=50`
      )
    : null;
  const items = rankingResponse?.data.result.items ?? [];

  return (
    <>
      <form action="/rankings" className="ranking-selector data-table">
        <div>
          <h1>价格排行</h1>
          <p>选择模型后查看当前最低价中转。价格单位为 USD / 1M tokens。</p>
        </div>
        <label className="ranking-selector__control">
          <span>
            <span>模型选择</span>
            <select className="form-input" defaultValue={selectedModel ?? ""} name="model">
              {models.map((model) => (
                <option key={model.modelSlug} value={model.modelSlug}>
                  {model.modelName || model.requestName || model.modelSlug}
                </option>
              ))}
            </select>
          </span>
          <button className="primary-button" type="submit">
            查看排行
          </button>
        </label>
      </form>

      <section className="data-table">
        <div className="overflow-x-auto">
          <table className="recommend-table">
            <thead>
              <tr>
                <th>站点</th>
                <th>折算价</th>
                <th>分数</th>
                <th>操作</th>
              </tr>
            </thead>
            <tbody>
              {items.length ? (
                items.map((item, index) => (
                  <tr key={`${item.siteSlug}-${index}`}>
                    <td>
                      <strong>#{index + 1} {item.siteName}</strong>
                      <span>{item.siteSlug}</span>
                    </td>
                    <td>
                      <strong>{money(item.effectiveInputPriceUsd)} / {money(item.effectiveOutputPriceUsd)}</strong>
                      <span>输入 / 输出</span>
                    </td>
                    <td>
                      <span className="status-pill" data-tone="neutral">
                        {score(trustScore(null, item.riskScore))}
                      </span>
                      <span>越高越可信</span>
                    </td>
                    <td>
                      <Link href={`/sites/${item.siteSlug}`} className="text-button">
                        查看站点
                      </Link>
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={4}>{selectedModel ? "暂无价格排行数据。" : "暂无可选模型。"}</td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </section>
    </>
  );
}
