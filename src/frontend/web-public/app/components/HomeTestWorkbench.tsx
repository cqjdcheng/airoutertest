import { RelayTestPanel } from "@/app/components/RelayTestPanel";

type PopularModel = {
  modelSlug: string;
  modelName: string;
};

export function HomeTestWorkbench({ popularModels = [] }: { popularModels?: PopularModel[] }) {
  const modelOptions = popularModels
    .filter((model) => model.modelSlug && model.modelName)
    .map((model, index) => ({
      label: model.modelName,
      slug: model.modelSlug,
      badge: index === 0 ? "HOT" : undefined
    }));

  return (
    <section className="home-workbench home-workbench--focused">
      <div className="section-heading">
        <div>
          <p className="eyebrow">Self Test</p>
          <h2 className="section-title">直接测试中转接口</h2>
        </div>
      </div>

      <div className="home-workbench__body">
        <RelayTestPanel compact showHistory={false} modelOptions={modelOptions} />
      </div>
    </section>
  );
}
