# Unity AI Chat Avatar 🤖🎮

Bu proje, **Unity tabanlı yapay zekâ destekli bir sohbet avatarı sistemi** geliştirmeyi amaçlamaktadır.  
Amaç, kullanıcı ile **doğal ve etkileşimli** bir şekilde iletişim kurabilen, görsel bir avatar üzerinden çalışan bir yapay zekâ altyapısı oluşturmaktır.

Proje; **Unity**, **C#**, **yapay zekâ mantığı** ve **etkileşimli arayüz sistemlerini** bir araya getirerek modern bir sohbet deneyimi sunmayı hedefler.

---

## 🎯 Projenin Amacı

Bu projenin temel amacı:
- Kullanıcı girdilerine anlamlı yanıtlar verebilen
- Genişletilebilir ve modüler yapıda
- Unity üzerinde çalışan
- Yapay zekâ ile desteklenmiş bir sohbet avatarı geliştirmektir

Bu repo, projeye ait **bireysel geliştirmeleri ve iyileştirmeleri** içermektedir.

---

## 🧠 Özellikler

- Yapay zekâ tabanlı sohbet mantığı
- Unity UI sistemi ile oluşturulmuş etkileşimli arayüz
- Prefab tabanlı avatar ve UI yapısı
- Geliştirilmeye açık mimari:
  - Sesli giriş (Speech-to-Text)
  - Sesli çıkış (Text-to-Speech)
  - VR / AR entegrasyonu

---

## 🛠️ Kullanılan Teknolojiler

- **Unity**
- **C#**
- **ShaderLab / HLSL**
- Unity UI System
- Prefab tabanlı proje mimarisi

---

## 📁 Proje Yapısı

```text
Assets/
 ├─ Scripts/
 ├─ Prefabs/
 ├─ Scenes/
 ├─ UI/
Packages/
ProjectSettings/
## ⚙️ Kurulum ve Çalıştırma Notları

Bu proje, Unity üzerinden çalıştırılmadan önce **sahne ve bileşen eşleştirmeleri** gerektirmektedir.

### 🖼️ UI ve Sahne Ayarları

Projeyi kendi bilgisayarınıza çektikten sonra:

- **Assets** klasörü içindeki  
  - `Canvas 1`
  - `Player 2`  
  nesneleri **Hierarchy** penceresine sürükleyip bırakmanız gerekmektedir.

Bu işlem yapıldığında:
- UI bileşenleri sahnede aktif hale gelir
- Avatar ile etkileşim sorunsuz şekilde çalışır

Bu adım test edilmiştir ve sistem bu şekilde doğru çalışmaktadır.

---

### 🤖 Yapay Zekâ Servisi (OpenRouter)

Projede yapay zekâ yanıtları için **OpenRouter API** kullanılmıştır.

> ⚠️ Güvenlik ve kota nedenleriyle **API Key paylaşılmamıştır**.

Bu nedenle:
- Kod içerisindeki API key alanı **bilinçli olarak boş bırakılmıştır**
- Projeyi çalıştırmak isteyen kullanıcıların:
  - Kendi OpenRouter hesabından API key alması
  - İlgili script dosyasına bu anahtarı eklemesi gerekmektedir

---

### 🎭 Animasyon Sistemi

Avatar animasyonları Unity **Animator** sistemi üzerinden kontrol edilmektedir.

Animasyonların çalışması için:
1. **Hierarchy** üzerinden `Player 2` seçilmelidir
2. **Animator** bileşeninde ilgili animasyonlar aktif durumdadır
3. `Player 2` seçildiğinde animasyonların düzgün çalıştığı gözlemlenmiştir

Bu yapı proje içerisinde test edilmiş ve stabil şekilde çalışmaktadır.

---

### 📝 Not

Bu repo, projenin **bireysel geliştirme ve entegrasyon** kısmını temsil etmektedir.  
API anahtarları ve bazı ayarlar **bilinçli olarak dışarıda bırakılmıştır**.
