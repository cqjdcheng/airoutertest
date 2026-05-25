import { BackLink, PublicHeader } from "@/app/components/PublicHeader";
import { PublicPageHero } from "@/app/components/PublicPageHero";

const contactPoints = [
  {
    title: "不收费，纯公益站",
    description: "这个站点不收会员费，也不卖服务，主要是为了整理信息、降低踩坑成本。"
  },
  {
    title: "测试需要少量余额",
    description: "如果需要我协助复现或排查问题，请提供一个账户，并保证账户里有少量金额用于测试。测试量通常很小，一般几块钱就够了。"
  },
  {
    title: "微信联系",
    description: "微信：cjd_lok"
  }
];

export default function ContactPage() {
  return (
    <main className="public-shell">
      <PublicHeader />

      <section className="public-container public-main">
        <BackLink />

        <PublicPageHero
          eyebrow="Contact"
          title="联系我"
          description="如果你想反馈问题、补充站点信息，或者需要我帮你做小量真实测试，可以直接联系我。先看下面这三点，再加微信会更高效。"
          aside={
            <div className="contact-aside">
              <div className="metric-card">
                <span>站点性质</span>
                <strong>纯公益</strong>
                <small>不收费，不做付费咨询。</small>
              </div>
              <div className="metric-card">
                <span>测试成本</span>
                <strong>几元即可</strong>
                <small>只需要足够复现问题的小额余额。</small>
              </div>
            </div>
          }
        />

        <section className="contact-grid" aria-label="联系说明">
          {contactPoints.map((item) => (
            <article key={item.title} className="contact-card">
              <span className="pill">{item.title}</span>
              <p>{item.description}</p>
            </article>
          ))}
        </section>

        <section className="contact-highlight">
          <div>
            <p className="eyebrow">WeChat</p>
            <h2 className="section-title">cjd_lok</h2>
            <p className="section-copy">
              加好友时建议顺手带上你的问题背景，例如站点地址、模型名称、报错现象，能省掉来回确认时间。
            </p>
          </div>
        </section>
      </section>
    </main>
  );
}
