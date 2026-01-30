# Security Guide / Güvenlik Rehberi

## English

### Environment Variables Setup

This project uses environment variables to keep sensitive credentials secure. **NEVER commit real credentials to git.**

#### 1. Create .env File

Copy the example file and fill in your actual credentials:

```bash
cp .env.example .env
```

#### 2. Fill in Your Credentials

Edit `.env` with your actual values:

```env
# PostgreSQL Database
POSTGRES_PASSWORD=your-strong-postgres-password

# RabbitMQ Message Broker
RABBITMQ_PASSWORD=your-strong-rabbitmq-password

# JWT Authentication (minimum 256 bits / 32 characters)
JWT_SECRET=your-super-secret-jwt-key-minimum-32-characters-required
JWT_ISSUER=kantarv2
JWT_AUDIENCE=kantarUsers

# AWS S3 Credentials
AWS_S3_ACCESS_KEY=AKIA...
AWS_S3_SECRET_KEY=your-aws-secret-key

# Email Settings
EMAIL_SENDER=your-email@gmail.com
EMAIL_PASSWORD=your-gmail-app-password

# Groq LLM API
GROQ_API_KEY=gsk_...

# Grafana Loki (Optional)
LOKI_URI=http://loki:3100
LOKI_USERNAME=
LOKI_PASSWORD=
```

#### 3. Generate Strong Secrets

**For JWT Secret** (minimum 32 characters):
```bash
# Using openssl
openssl rand -base64 32

# Using PowerShell (Windows)
[System.Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

**For PostgreSQL/RabbitMQ passwords**:
```bash
# Using openssl
openssl rand -base64 24
```

#### 4. Security Checklist

- [ ] `.env` file is listed in `.gitignore`
- [ ] Never commit real credentials to git
- [ ] Use strong, unique passwords for each service
- [ ] JWT secret is at least 32 characters (256 bits)
- [ ] Use Gmail App Passwords, not your regular password
- [ ] Rotate credentials regularly
- [ ] Use different credentials for development and production

#### 5. Running with Docker Compose

Docker Compose will automatically load variables from `.env`:

```bash
docker compose up -d
```

### What's Already Secured

- `appsettings.json` - All sensitive fields are empty strings
- `appsettings.Development.json` - No real credentials
- `docker-compose.yml` - Uses environment variables
- `.gitignore` - Blocks `.env` and `appsettings.Production.json`

### Before Making Repository Public

1. Verify no credentials in git history:
```bash
git log --all -S "password" --pretty=format:"%H %s"
git log --all -S "secret" --pretty=format:"%H %s"
```

2. Remove old credentials from git history if needed (advanced):
```bash
# Use BFG Repo-Cleaner or git-filter-repo
# Only do this if absolutely necessary
```

3. Change all production credentials
4. Review all configuration files
5. Double-check `.gitignore`

---

## Türkçe

### Ortam Değişkenleri Kurulumu

Bu proje, hassas kimlik bilgilerini güvende tutmak için ortam değişkenlerini kullanır. **ASLA gerçek kimlik bilgilerini git'e commit ETMEYİN.**

#### 1. .env Dosyası Oluşturun

Örnek dosyayı kopyalayın ve gerçek kimlik bilgilerinizi doldurun:

```bash
cp .env.example .env
```

#### 2. Kimlik Bilgilerinizi Doldurun

`.env` dosyasını gerçek değerlerinizle düzenleyin:

```env
# PostgreSQL Veritabanı
POSTGRES_PASSWORD=guclu-postgres-sifreniz

# RabbitMQ Mesaj Aracısı
RABBITMQ_PASSWORD=guclu-rabbitmq-sifreniz

# JWT Kimlik Doğrulama (minimum 256 bit / 32 karakter)
JWT_SECRET=super-gizli-jwt-anahtari-minimum-32-karakter-gerekli
JWT_ISSUER=kantarv2
JWT_AUDIENCE=kantarUsers

# AWS S3 Kimlik Bilgileri
AWS_S3_ACCESS_KEY=AKIA...
AWS_S3_SECRET_KEY=aws-secret-anahtariniz

# E-posta Ayarları
EMAIL_SENDER=email-adresiniz@gmail.com
EMAIL_PASSWORD=gmail-uygulama-sifreniz

# Groq LLM API
GROQ_API_KEY=gsk_...

# Grafana Loki (Opsiyonel)
LOKI_URI=http://loki:3100
LOKI_USERNAME=
LOKI_PASSWORD=
```

#### 3. Güçlü Şifreler Oluşturun

**JWT Secret için** (minimum 32 karakter):
```bash
# openssl kullanarak
openssl rand -base64 32

# PowerShell (Windows) kullanarak
[System.Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

**PostgreSQL/RabbitMQ şifreleri için**:
```bash
# openssl kullanarak
openssl rand -base64 24
```

#### 4. Güvenlik Kontrol Listesi

- [ ] `.env` dosyası `.gitignore` içinde listelenmiş
- [ ] Asla gerçek kimlik bilgilerini git'e commit etmeyin
- [ ] Her servis için güçlü, benzersiz şifreler kullanın
- [ ] JWT secret en az 32 karakter (256 bit)
- [ ] Normal şifrenizi değil, Gmail Uygulama Şifresi kullanın
- [ ] Kimlik bilgilerini düzenli olarak yenileyin
- [ ] Geliştirme ve production için farklı kimlik bilgileri kullanın

#### 5. Docker Compose ile Çalıştırma

Docker Compose, `.env` dosyasındaki değişkenleri otomatik yükler:

```bash
docker compose up -d
```

### Zaten Güvenli Olan Kısımlar

- `appsettings.json` - Tüm hassas alanlar boş string
- `appsettings.Development.json` - Gerçek kimlik bilgisi yok
- `docker-compose.yml` - Ortam değişkenlerini kullanıyor
- `.gitignore` - `.env` ve `appsettings.Production.json` engellenmiş

### Repository'yi Public Yapmadan Önce

1. Git geçmişinde kimlik bilgisi olmadığını doğrulayın:
```bash
git log --all -S "password" --pretty=format:"%H %s"
git log --all -S "secret" --pretty=format:"%H %s"
```

2. Gerekirse eski kimlik bilgilerini git geçmişinden kaldırın (ileri düzey):
```bash
# BFG Repo-Cleaner veya git-filter-repo kullanın
# Bunu sadece kesinlikle gerekiyorsa yapın
```

3. Tüm production kimlik bilgilerini değiştirin
4. Tüm yapılandırma dosyalarını gözden geçirin
5. `.gitignore` dosyasını iki kez kontrol edin

## Important Notes / Önemli Notlar

- `.env` files are automatically ignored by git (check `.gitignore`)
- `appsettings.Production.json` is also ignored
- Never share your `.env` file with anyone
- Use different credentials for each environment (dev, staging, production)
- Regularly rotate your secrets, especially after team member changes

---

- `.env` dosyaları git tarafından otomatik olarak göz ardı edilir (`.gitignore` kontrol edin)
- `appsettings.Production.json` de göz ardı edilir
- `.env` dosyanızı asla kimseyle paylaşmayın
- Her ortam için farklı kimlik bilgileri kullanın (dev, staging, production)
- Özellikle ekip üyesi değişikliklerinden sonra şifrelerinizi düzenli olarak yenileyin
