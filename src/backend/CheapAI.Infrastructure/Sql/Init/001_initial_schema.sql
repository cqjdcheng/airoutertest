CREATE TABLE IF NOT EXISTS admin_users (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  username VARCHAR(64) NOT NULL,
  password_hash VARCHAR(255) NOT NULL,
  display_name VARCHAR(64) NOT NULL,
  email VARCHAR(128) NULL,
  phone VARCHAR(32) NULL,
  status VARCHAR(24) NOT NULL DEFAULT 'active',
  last_login_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_admin_users_username (username)
);
--//@
CREATE TABLE IF NOT EXISTS refresh_tokens (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  admin_user_id BIGINT UNSIGNED NOT NULL,
  token_hash CHAR(64) NOT NULL,
  user_agent VARCHAR(255) NULL,
  ip_address VARCHAR(64) NULL,
  expires_at DATETIME(3) NOT NULL,
  revoked_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_refresh_tokens_token_hash (token_hash),
  KEY idx_refresh_tokens_admin_user_id (admin_user_id)
);
--//@
CREATE TABLE IF NOT EXISTS relay_sites (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  slug VARCHAR(128) NOT NULL,
  name VARCHAR(128) NOT NULL,
  base_url VARCHAR(255) NOT NULL,
  website_url VARCHAR(255) NULL,
  description TEXT NULL,
  supports_refund TINYINT(1) NOT NULL DEFAULT 0,
  supports_invoice TINYINT(1) NOT NULL DEFAULT 0,
  has_docs TINYINT(1) NOT NULL DEFAULT 0,
  docs_url VARCHAR(255) NULL,
  site_created_at DATETIME(3) NULL,
  first_tracked_at DATETIME(3) NULL,
  status VARCHAR(24) NOT NULL DEFAULT 'draft',
  invite_url VARCHAR(255) NULL,
  recent_review TEXT NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  deleted_at DATETIME(3) NULL,
  created_by BIGINT UNSIGNED NULL,
  updated_by BIGINT UNSIGNED NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_relay_sites_slug (slug),
  KEY idx_relay_sites_status (status),
  KEY idx_relay_sites_deleted_at (deleted_at)
);
--//@
CREATE TABLE IF NOT EXISTS site_channels (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_id BIGINT UNSIGNED NOT NULL,
  code VARCHAR(64) NOT NULL,
  name VARCHAR(128) NOT NULL,
  upstream_type VARCHAR(64) NULL,
  route_hint VARCHAR(255) NULL,
  status VARCHAR(24) NOT NULL DEFAULT 'active',
  is_public_visible TINYINT(1) NOT NULL DEFAULT 0,
  notes TEXT NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  deleted_at DATETIME(3) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_site_channels_site_code (site_id, code),
  KEY idx_site_channels_site_id (site_id)
);
--//@
CREATE TABLE IF NOT EXISTS models (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  slug VARCHAR(128) NOT NULL,
  vendor VARCHAR(64) NOT NULL,
  official_model_id VARCHAR(128) NOT NULL,
  display_name VARCHAR(128) NOT NULL,
  description TEXT NULL,
  status VARCHAR(24) NOT NULL DEFAULT 'active',
  official_input_price_usd DECIMAL(18, 6) NULL,
  official_output_price_usd DECIMAL(18, 6) NULL,
  capability_score DECIMAL(10, 4) NULL,
  capability_source VARCHAR(64) NULL,
  capability_updated_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  deleted_at DATETIME(3) NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uk_models_slug (slug),
  UNIQUE KEY uk_models_vendor_official_id (vendor, official_model_id)
);
--//@
CREATE TABLE IF NOT EXISTS model_ranking_snapshots (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  ranking_type VARCHAR(24) NOT NULL,
  window_type VARCHAR(16) NOT NULL,
  model_id BIGINT UNSIGNED NOT NULL,
  site_id BIGINT UNSIGNED NOT NULL,
  rank_position INT NOT NULL,
  effective_input_price_usd DECIMAL(18, 6) NULL,
  effective_output_price_usd DECIMAL(18, 6) NULL,
  availability_score DECIMAL(10, 4) NULL,
  stability_score DECIMAL(10, 4) NULL,
  speed_score DECIMAL(10, 4) NULL,
  risk_score DECIMAL(10, 4) NULL,
  final_score DECIMAL(10, 4) NULL,
  snapshot_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_model_rankings_query (ranking_type, window_type, model_id, snapshot_at, rank_position)
);
--//@
CREATE TABLE IF NOT EXISTS relay_offers (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_id BIGINT UNSIGNED NOT NULL,
  model_id BIGINT UNSIGNED NOT NULL,
  channel_id BIGINT UNSIGNED NULL,
  source_type VARCHAR(24) NOT NULL DEFAULT 'manual',
  currency VARCHAR(8) NOT NULL DEFAULT 'USD',
  official_input_price_usd DECIMAL(18, 6) NULL,
  official_output_price_usd DECIMAL(18, 6) NULL,
  site_input_price_usd DECIMAL(18, 6) NULL,
  site_output_price_usd DECIMAL(18, 6) NULL,
  recharge_ratio DECIMAL(18, 6) NOT NULL DEFAULT 1,
  bonus_ratio DECIMAL(18, 6) NOT NULL DEFAULT 0,
  effective_input_price_usd DECIMAL(18, 6) NULL,
  effective_output_price_usd DECIMAL(18, 6) NULL,
  status VARCHAR(24) NOT NULL DEFAULT 'active',
  crawled_at DATETIME(3) NULL,
  reviewed_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_relay_offers_site_model_source (site_id, model_id, source_type),
  KEY idx_relay_offers_model_status (model_id, status),
  KEY idx_relay_offers_site_status (site_id, status)
);
--//@
CREATE TABLE IF NOT EXISTS test_records (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_id BIGINT UNSIGNED NOT NULL,
  model_id BIGINT UNSIGNED NOT NULL,
  channel_id BIGINT UNSIGNED NULL,
  test_type VARCHAR(24) NOT NULL DEFAULT 'platform',
  status VARCHAR(24) NOT NULL,
  first_token_ms INT NULL,
  full_response_ms INT NULL,
  error_code VARCHAR(64) NULL,
  error_message VARCHAR(255) NULL,
  prompt_hash CHAR(64) NULL,
  response_hash CHAR(64) NULL,
  detected_model_id VARCHAR(128) NULL,
  tested_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_test_records_site_model_time (site_id, model_id, tested_at),
  KEY idx_test_records_type_status (test_type, status)
);
--//@
CREATE TABLE IF NOT EXISTS risk_evidences (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_id BIGINT UNSIGNED NOT NULL,
  model_id BIGINT UNSIGNED NOT NULL,
  test_record_id BIGINT UNSIGNED NULL,
  rule_code VARCHAR(64) NOT NULL,
  risk_level VARCHAR(24) NOT NULL,
  risk_score DECIMAL(10, 4) NOT NULL DEFAULT 0,
  evidence_summary VARCHAR(512) NOT NULL,
  evidence_json JSON NULL,
  review_status VARCHAR(24) NOT NULL DEFAULT 'pending',
  reviewed_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_risk_evidences_site_model_rule (site_id, model_id, rule_code),
  KEY idx_risk_evidences_level_status (risk_level, review_status),
  KEY idx_risk_evidences_site_model (site_id, model_id)
);
--//@
CREATE TABLE IF NOT EXISTS crawl_jobs (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_id BIGINT UNSIGNED NULL,
  job_type VARCHAR(32) NOT NULL DEFAULT 'manual_price_crawl',
  status VARCHAR(24) NOT NULL DEFAULT 'pending',
  priority INT NOT NULL DEFAULT 5,
  requested_by BIGINT UNSIGNED NULL,
  started_at DATETIME(3) NULL,
  finished_at DATETIME(3) NULL,
  error_message VARCHAR(512) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_crawl_jobs_site_status (site_id, status),
  KEY idx_crawl_jobs_created_at (created_at)
);
--//@
CREATE TABLE IF NOT EXISTS test_jobs (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_id BIGINT UNSIGNED NULL,
  model_id BIGINT UNSIGNED NULL,
  job_type VARCHAR(32) NOT NULL DEFAULT 'platform_model_test',
  status VARCHAR(24) NOT NULL DEFAULT 'pending',
  priority INT NOT NULL DEFAULT 5,
  requested_by BIGINT UNSIGNED NULL,
  started_at DATETIME(3) NULL,
  finished_at DATETIME(3) NULL,
  error_message VARCHAR(512) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_test_jobs_site_model_status (site_id, model_id, status),
  KEY idx_test_jobs_created_at (created_at)
);
--//@
CREATE TABLE IF NOT EXISTS aggregate_jobs (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  aggregate_type VARCHAR(32) NOT NULL DEFAULT 'ranking_snapshot',
  status VARCHAR(24) NOT NULL DEFAULT 'pending',
  requested_by BIGINT UNSIGNED NULL,
  started_at DATETIME(3) NULL,
  finished_at DATETIME(3) NULL,
  error_message VARCHAR(512) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_aggregate_jobs_type_status (aggregate_type, status),
  KEY idx_aggregate_jobs_created_at (created_at)
);
--//@
CREATE TABLE IF NOT EXISTS job_execution_logs (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  job_category VARCHAR(32) NOT NULL,
  job_id BIGINT UNSIGNED NULL,
  status VARCHAR(24) NOT NULL,
  message VARCHAR(512) NOT NULL,
  started_at DATETIME(3) NULL,
  finished_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_job_execution_logs_category_time (job_category, created_at)
);
--//@
CREATE TABLE IF NOT EXISTS self_tests (
  id CHAR(36) NOT NULL,
  site_url VARCHAR(255) NOT NULL,
  model_name VARCHAR(128) NOT NULL,
  test_mode VARCHAR(32) NOT NULL DEFAULT 'basic',
  is_stream TINYINT(1) NOT NULL DEFAULT 1,
  status VARCHAR(24) NOT NULL DEFAULT 'succeeded',
  first_token_ms INT NULL,
  full_response_ms INT NULL,
  risk_score DECIMAL(10, 4) NOT NULL DEFAULT 0,
  risk_level VARCHAR(24) NOT NULL DEFAULT 'low',
  result_summary VARCHAR(512) NOT NULL,
  match_score DECIMAL(10, 4) NOT NULL DEFAULT 0,
  input_tokens INT NULL,
  output_tokens INT NULL,
  total_tokens INT NULL,
  estimated_tokens INT NOT NULL DEFAULT 1000,
  tokens_per_second DECIMAL(10, 2) NULL,
  checks_json JSON NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  expires_at DATETIME(3) NOT NULL,
  PRIMARY KEY (id),
  KEY idx_self_tests_created_at (created_at)
);
--//@
CREATE TABLE IF NOT EXISTS site_submissions (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  site_name VARCHAR(128) NOT NULL,
  site_url VARCHAR(255) NOT NULL,
  contact VARCHAR(128) NULL,
  description TEXT NULL,
  review_status VARCHAR(24) NOT NULL DEFAULT 'pending',
  review_note VARCHAR(512) NULL,
  reviewed_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  KEY idx_site_submissions_review_status (review_status, created_at)
);
--//@
CREATE TABLE IF NOT EXISTS model_capability_snapshots (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  model_id BIGINT UNSIGNED NOT NULL,
  source VARCHAR(64) NOT NULL,
  rank_position INT NOT NULL,
  capability_score DECIMAL(10, 4) NOT NULL,
  snapshot_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_model_capability_source_model (source, model_id),
  KEY idx_model_capability_source_rank (source, rank_position)
);
--//@
CREATE TABLE IF NOT EXISTS article_categories (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  slug VARCHAR(128) NOT NULL,
  name VARCHAR(128) NOT NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_article_categories_slug (slug)
);
--//@
CREATE TABLE IF NOT EXISTS articles (
  id BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  category_id BIGINT UNSIGNED NULL,
  slug VARCHAR(128) NOT NULL,
  title VARCHAR(160) NOT NULL,
  summary VARCHAR(512) NULL,
  content_md MEDIUMTEXT NOT NULL,
  status VARCHAR(24) NOT NULL DEFAULT 'draft',
  published_at DATETIME(3) NULL,
  created_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3),
  updated_at DATETIME(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3) ON UPDATE CURRENT_TIMESTAMP(3),
  PRIMARY KEY (id),
  UNIQUE KEY uk_articles_slug (slug),
  KEY idx_articles_status_published_at (status, published_at)
);
--//@
INSERT INTO relay_sites (
  slug,
  name,
  base_url,
  website_url,
  description,
  supports_refund,
  supports_invoice,
  has_docs,
  docs_url,
  status,
  invite_url,
  recent_review
)
SELECT
  'relay-port',
  'RelayPort',
  'https://api.relayport.example/v1',
  'https://relayport.example',
  '示例中转站，用于本地开发预览。',
  1,
  1,
  1,
  'https://relayport.example/docs',
  'active',
  'https://relayport.example/invite/demo',
  '本地预览示例站点'
WHERE NOT EXISTS (
  SELECT 1 FROM relay_sites WHERE slug = 'relay-port'
);
--//@
INSERT INTO models (
  slug,
  vendor,
  official_model_id,
  display_name,
  description,
  status,
  official_input_price_usd,
  official_output_price_usd
)
SELECT
  'gpt-4-1-mini',
  'OpenAI',
  'gpt-4.1-mini',
  'GPT-4.1 mini',
  '本地预览示例模型',
  'active',
  0.40,
  1.60
WHERE NOT EXISTS (
  SELECT 1 FROM models WHERE slug = 'gpt-4-1-mini'
);
--//@
INSERT INTO model_ranking_snapshots (
  ranking_type,
  window_type,
  model_id,
  site_id,
  rank_position,
  effective_input_price_usd,
  effective_output_price_usd,
  availability_score,
  stability_score,
  speed_score,
  risk_score,
  final_score
)
SELECT
  'price',
  '7d',
  m.id,
  s.id,
  1,
  0.32,
  1.27,
  98.8,
  97.9,
  82.0,
  14.0,
  92.0
FROM models m
JOIN relay_sites s ON s.slug = 'relay-port'
WHERE m.slug = 'gpt-4-1-mini'
  AND NOT EXISTS (
    SELECT 1
    FROM model_ranking_snapshots x
    WHERE x.ranking_type = 'price'
      AND x.window_type = '7d'
      AND x.model_id = m.id
      AND x.site_id = s.id
  );
--//@
INSERT INTO article_categories (slug, name)
SELECT 'guide', '使用指南'
WHERE NOT EXISTS (SELECT 1 FROM article_categories WHERE slug = 'guide');
--//@
INSERT INTO articles (
  category_id,
  slug,
  title,
  summary,
  content_md,
  status,
  published_at
)
SELECT
  c.id,
  'cheapai-data-policy',
  'CheapAI 数据口径说明',
  '说明实际折算价、平台测试和风险分的基础口径。',
  'CheapAI 公共排行只使用平台定时测试数据；价格以实际折算价为主；风险分采用规则累加并保留证据。',
  'published',
  CURRENT_TIMESTAMP(3)
FROM article_categories c
WHERE c.slug = 'guide'
  AND NOT EXISTS (SELECT 1 FROM articles WHERE slug = 'cheapai-data-policy');
--//@
INSERT INTO model_capability_snapshots (
  model_id,
  source,
  rank_position,
  capability_score
)
SELECT
  m.id,
  'Artificial Analysis',
  1,
  86.5
FROM models m
WHERE m.slug = 'gpt-4-1-mini'
  AND NOT EXISTS (
    SELECT 1 FROM model_capability_snapshots x
    WHERE x.source = 'Artificial Analysis' AND x.model_id = m.id
  );
--//@
INSERT INTO model_ranking_snapshots (
  ranking_type,
  window_type,
  model_id,
  site_id,
  rank_position,
  effective_input_price_usd,
  effective_output_price_usd,
  availability_score,
  stability_score,
  speed_score,
  risk_score,
  final_score
)
SELECT
  'stability',
  '7d',
  m.id,
  s.id,
  1,
  0.32,
  1.27,
  98.8,
  97.9,
  82.0,
  14.0,
  95.0
FROM models m
JOIN relay_sites s ON s.slug = 'relay-port'
WHERE m.slug = 'gpt-4-1-mini'
  AND NOT EXISTS (
    SELECT 1
    FROM model_ranking_snapshots x
    WHERE x.ranking_type = 'stability'
      AND x.window_type = '7d'
      AND x.model_id = m.id
      AND x.site_id = s.id
  );
