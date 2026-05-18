import { RelayTestPanel } from "@/app/components/RelayTestPanel";

type PopularModel = {
  modelSlug: string;
  modelName: string;
  requestName?: string;
  apiType?: "openai" | "anthropic";
};

export function HomeTestWorkbench({ popularModels = [] }: { popularModels?: PopularModel[] }) {
  const modelOptions = popularModels
    .filter((model) => model.modelSlug && model.modelName)
    .map((model, index) => ({
      label: model.modelName,
      slug: model.modelSlug,
      requestName: model.requestName || model.modelSlug,
      apiType: model.apiType ?? "openai",
      badge: index === 0 ? "HOT" : undefined
    }));

  return (
    <section className="home-workbench">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Self Test</p>
          <h2 className="section-title">直接测试中转接口</h2>
          <p className="section-copy">填写接口地址、Key 和目标模型，立即发起一次真实连通性与风险检测。站长保证不保存任何信息，但是还是建议您申请一个临时key用完后销毁。</p>
        </div>
      </div>

      <div className="market-panel home-workbench__panel">
        <RelayTestPanel compact showHistory={false} modelOptions={modelOptions} />
      </div>
    </section>
  );
}
