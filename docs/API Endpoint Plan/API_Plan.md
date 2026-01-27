# SnakeAid API Implementation Plan

This document outlines the proposed API endpoints for the SnakeAid platform, derived from the functional specifications and database design.

## 1. Authentication
*Manage user identity and sessions.*

### User (Common)
*Endpoints available to all authenticated users or guests.*

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/auth/register` | Register a new account. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/auth/login` | Login and receive JWT. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/auth/refresh` | Refresh access token. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/auth/google` | Login/Register with Google. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/auth/logout` | Logout. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/auth/verify-account` | Verify account using OTP. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **GET** | `/api/auth/me` | Get current user info. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/email/send-otp` | Send OTP to email. | ✅ Implemented | feature/SA002-ASPI_Email_Intergration |
| **POST** | `/api/otp/check` | Check OTP validity. | ✅ Implemented | feature/SA001-ASPNET_Identity |
| **POST** | `/api/otp/validate` | Validate and consume OTP. | ✅ Implemented | feature/SA001-ASPNET_Identity |

#### Password Management (Planned)

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/auth/forgot-password` | Initiate password reset. | 📝 Planned | - |
| **POST** | `/api/auth/reset-password` | Complete password reset. | 📝 Planned | - |

---

## 2. Account Profiles
*Manage separate profiles for Members, Rescuers, and Experts.*

### Member

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/members/me` | Get current user's member profile. | 📝 Planned | - |
| **PUT** | `/api/members/me` | Update member profile (emergency contact, etc.). | 📝 Planned | - |

### Rescuer

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/rescuers/me` | Get current user's rescuer profile. | 📝 Planned | - |
| **PUT** | `/api/rescuers/me` | Update rescuer profile (bio, availability, type). | 📝 Planned | - |
| **PUT** | `/api/rescuers/me/location` | Update real-time location. | 📝 Planned | - |

### Expert

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/experts/me` | Get current user's expert profile. | 📝 Planned | - |
| **PUT** | `/api/experts/me` | Update expert profile (bio, specialization). | 📝 Planned | - |
| **GET** | `/api/experts/me/certificates` | List my certificates. | 📝 Planned | - |
| **POST** | `/api/experts/me/certificates` | Upload/Submit a new certificate. | 📝 Planned | - |

#### Public Info

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/experts/{expertId}/certificates` | View approved certificates of an expert. | 📝 Planned | - |

---

## 3. Emergency (SOS)
*Critical snakebite incidents and rescue coordination.*

### Incidents (Patient)

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/incidents/sos` | Create a snakebite emergency (Location, Symptoms, Images). | 📝 Planned | - |
| **GET** | `/api/incidents/{id}` | Get incident details and status. | 📝 Planned | - |
| **PUT** | `/api/incidents/{id}/symptoms` | Update symptoms & images (Recalculate severity). | 📝 Planned | - |
| **PUT** | `/api/incidents/{id}/cancel` | Cancel the SOS. | 📝 Planned | - |

### Rescue Requests (Rescuer)

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/incidents/nearby` | List active SOS requests nearby (Rescuer). | 📝 Planned | - |
| **POST** | `/api/incidents/{id}/accept` | Accept an SOS request (Creates a `RescueMission`). | 📝 Planned | - |

### Missions (Active Rescue)

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/missions/active` | Get current active mission (for both Patient and Rescuer). | 📝 Planned | - |
| **PUT** | `/api/missions/{id}/status` | Update status (EnRoute, Arrived, Hospital, Completed). | 📝 Planned | - |
| **POST** | `/api/missions/{id}/location` | Stream location updates during mission (Rescuer). | 📝 Planned | - |
| **GET** | `/api/missions/{id}/tracking` | Get stream of rescuer location (Patient). | 📝 Planned | - |

---

## 4. Snake Catching Service (Removal)
*Non-emergency snake removal service.*

### Requests

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/catching/requests` | Request snake removal (Location, Declared Species/Size). | 📝 Planned | - |
| **GET** | `/api/catching/requests/me` | List my requests. | 📝 Planned | - |
| **GET** | `/api/catching/requests/nearby` | Find removal jobs (Rescuer). | 📝 Planned | - |

### Missions

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/catching/requests/{id}/accept` | Accept removal job. | 📝 Planned | - |
| **PUT** | `/api/catching/missions/{id}/report` | Report actual species/size found (Rescuer). | 📝 Planned | - |
| **PUT** | `/api/catching/missions/{id}/complete` | Mark job as completed. | 📝 Planned | - |

---

## 5. Snake Library & Knowledge Base
*Public access to snake data, identification, and first aid.*

### Snake Species

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/snakes` | List/Search snakes (filters: venomous, family, location). | 📝 Planned | - |
| **GET** | `/api/snakes/{slug}` | Get details of a specific snake (info, images, venom). | 📝 Planned | - |
| **GET** | `/api/snakes/{slug}/distribution` | Get distribution map/polygon data. | 📝 Planned | - |

### First Aid

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/first-aid/general` | Get general first aid guidelines. | 📝 Planned | - |
| **GET** | `/api/first-aid/species/{slug}` | Get specific guidelines for a snake species. | 📝 Planned | - |

### Venoms & Antivenoms

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/venoms` | List venom types. | 📝 Planned | - |
| **GET** | `/api/antivenoms` | List antivenom types and details. | 📝 Planned | - |
| **GET** | `/api/antivenoms/nearest` | Find facilities with antivenom near a location (lat, long). | 📝 Planned | - |

### Admin (Content Management)

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/admin/snakes` | Create species. | 📝 Planned | - |
| **PUT** | `/api/admin/snakes/{id}` | Update species. | 📝 Planned | - |
| **POST** | `/api/admin/antivenoms` | Manage antivenom data. | 📝 Planned | - |

---

## 6. AI Identification
*AI-powered recognition and expert verification.*

### Prediction

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/identification/health` | Check SnakeAI service status & model info. | 📝 Planned | feature/SA004-SnakeAI_Intergration |
| **POST** | `/api/identification/detect` | Upload image for identification (Wraps SnakeAI). | 📝 Planned | feature/SA004-SnakeAI_Intergration |
| **GET** | `/api/identification/{id}` | Get result of a specific identification session. | 📝 Planned | feature/SA004-SnakeAI_Intergration |

### Expert Verification

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/identification/{id}/request-verification` | Request expert review for an AI result. | 📝 Planned | - |
| **POST** | `/api/identification/{id}/verify` | Confirm or correct the species (Expert Only). | 📝 Planned | - |

---

## 7. Expert Consultation
*Booking and conducting consultations.*

### Booking

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/experts` | Find experts (filter by specialization, rating). | 📝 Planned | - |
| **GET** | `/api/experts/{id}/availability` | Get available time slots. | 📝 Planned | - |
| **POST** | `/api/consultations/book` | Book a specific slot. | 📝 Planned | - |

### Sessions

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/consultations/active` | Get ongoing/upcoming session. | 📝 Planned | - |
| **GET** | `/api/consultations/{id}/join` | Get WebRTC room details/token. | 📝 Planned | - |
| **PUT** | `/api/consultations/{id}/cancel` | Cancel booking. | 📝 Planned | - |

---

## 8. Community & Map

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/community/reports` | Report a snake sighting (Public). | 📝 Planned | - |
| **GET** | `/api/community/reports` | Get nearby sightings (Heatmap data). | 📝 Planned | - |
| **GET** | `/api/facilities` | List medical facilities/hospitals. | 📝 Planned | - |

---

## 9. Wallet & Payments

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **GET** | `/api/wallet/balance` | Get current balance. | 📝 Planned | - |
| **GET** | `/api/wallet/transactions` | History. | 📝 Planned | - |
| **POST** | `/api/wallet/deposit` | Initiate deposit (Momo/ZaloPay/Bank). | 📝 Planned | - |
| **POST** | `/api/wallet/withdraw` | Request withdrawal (Experts/Rescuers). | 📝 Planned | - |

---

## 10. Media & Uploads
*File and image management.*

| | | | | |
| :--- | :--- | :--- | :--- | :--- |
| **POST** | `/api/media/upload-image` | Upload an image. | ✅ Implemented | feature/SA004-SnakeAI_Intergration |
| **POST** | `/api/media/upload-file` | Upload a file. | ✅ Implemented | feature/SA004-SnakeAI_Intergration |
