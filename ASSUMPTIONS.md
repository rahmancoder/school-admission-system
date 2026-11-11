# Assumptions

এই প্রজেক্টটি বাস্তবায়নের সময় নিচের প্রযুক্তিগত ও কার্যগত অনুমানগুলো গ্রহণ করা হয়েছে।

### ১. EF Core (Entity Framework Core)

EF Core হলো Microsoft-এর তৈরি একটি ওপেন-সোর্স, ক্রস-প্ল্যাটফর্ম Object-Relational Mapper (ORM), যা .NET অ্যাপ্লিকেশনকে রিলেশনাল ডাটাবেসের সঙ্গে কাজ করতে সাহায্য করে। এটি C# অবজেক্ট এবং LINQ ব্যবহার করে ডেটা পরিচালনার সুযোগ দেয়। প্রয়োজনে Raw SQL Query-ও ব্যবহার করা যায়।

### ২. মাইগ্রেশনের পরিবর্তে EnsureCreated ব্যবহার

One-command Docker startup সহজ রাখার জন্য ডাটাবেস স্কিমা তৈরিতে EF Core-এর `EnsureCreated` ব্যবহার করা হয়েছে। এটি নতুন ডাটাবেসে প্রয়োজনীয় টেবিল তৈরি করে। তবে ভবিষ্যতে স্কিমা পরিবর্তন ও ভার্সন নিয়ন্ত্রণের জন্য Production Environment-এ EF Core Migrations ব্যবহার করা উচিত।

### ৩. কোনো প্রকৃত Authentication System নেই

এই প্রজেক্টে কোনো Login বা Authentication System বাস্তবায়ন করা হয়নি। `X-Student-Id` হেডারটি বিশ্বস্ত হিসেবে বিবেচনা করা হয় এবং এটি শুধুমাত্র ১৭ ডিজিটের কি না তা যাচাই করা হয়। এই যাচাই শিক্ষার্থীর প্রকৃত পরিচয় নিশ্চিত করে না।

- `POST /classes/{classId}/holds` এবং `GET /me/holds`-এর জন্য `X-Student-Id` বাধ্যতামূলক।
- `GET /holds/{id}`, Confirm এবং Cancel Request-এ হেডারটি ঐচ্ছিক রাখা হয়েছে, যাতে ম্যানুয়ালি API পরীক্ষা করা যায়।
- হেডার পাঠানো হলে এবং সেটি Hold-এর মালিকের Student ID-এর সঙ্গে না মিললে API `403 Forbidden` রিটার্ন করে।

Production Environment-এ প্রকৃত Authentication ও Authorization ব্যবস্থা প্রয়োজন।

### ৪. Hold করা Seat-ও Capacity-এর মধ্যে গণ্য হবে

একটি ক্লাসের ক্ষেত্রে মোট Hold করা এবং Confirmed Seat-এর সংখ্যা কখনোই মোট Seat Capacity অতিক্রম করতে পারবে না।

`Active Seats = Unexpired HELD Seats + CONFIRMED Seats`

কোনো HELD Seat-এর মেয়াদ শেষ হয়ে গেলে Background Sweeper সেটিকে `EXPIRED` স্ট্যাটাসে আপডেট করার আগেই সেটি Seat Availability গণনা থেকে বাদ পড়ে যায়।

### ৫. Active Application-এর সংজ্ঞা

একটি Application-কে Active হিসেবে বিবেচনা করা হবে যদি সেটির স্ট্যাটাস হয়:

- `HELD` এবং Hold-এর মেয়াদ এখনো শেষ হয়নি।
- `CONFIRMED`।

কোনো শিক্ষার্থীর আগের Application `CANCELLED` অথবা `EXPIRED` হলে সে একই ক্লাসে আবার আবেদন করতে পারবে।

### ৬. Application Cancellation এবং Idempotency

শুধুমাত্র `HELD` Application Cancel করা যাবে। `CONFIRMED` Application Cancel করার অনুমতি নেই, কারণ Admission Cancellation ও Refund Policy প্রজেক্টের নির্ধারিত Requirements-এ উল্লেখ করা হয়নি। এ ক্ষেত্রে API `409 Conflict` রিটার্ন করে।

একই Application-এর ক্ষেত্রে সেটি ইতোমধ্যে যে স্ট্যাটাসে রয়েছে, সেই একই Confirm বা Cancel Operation আবার পাঠানো হলে সেটি Idempotent হিসেবে বিবেচিত হবে। তবে অন্য কোনো অবৈধ State Transition অনুমোদিত নয়।

### ৭. Payment System সিমুলেটেড

এই প্রজেক্টে কোনো বাস্তব Payment Gateway সংযুক্ত করা হয়নি। Payment Verification-এর পরিবর্তে Application Confirmation সিমুলেট করা হয়েছে।

একটি Hold এখনো `HELD` অবস্থায় থাকলে এবং তার মেয়াদ শেষ না হলে `POST /holds/{id}/confirm` Request সফল হতে পারে।

### ৮. Class Name এবং Seat Capacity

প্রতিটি Class-এর নাম Unique হতে হবে। একই নামে একাধিক Class তৈরি করার চেষ্টা করলে API `409 Conflict` রিটার্ন করবে।

Class তৈরি হওয়ার পর তার Seat Capacity পরিবর্তন করার কোনো API এই প্রজেক্টে রাখা হয়নি।

### ৯. Class Creation-এর জন্য Admin Role নেই

প্রজেক্টে কোনো Login বা Role-Based Access Control (RBAC) ব্যবস্থা নেই। তাই যেকোনো API Client Class তৈরি করতে পারবে। Class Creation-এর জন্য আলাদা Admin Authorization বাস্তবায়ন করা হয়নি।

### ১০. Hold Duration Configuration

Hold-এর সময়সীমা অ্যাপ্লিকেশন চালু হওয়ার সময় `HOLD_DURATION_SECONDS` Environment Variable থেকে একবার পড়া হয়। এর Default Value হলো `600` সেকেন্ড (১০ মিনিট)।

এই Environment Variable পরিবর্তন করলে Backend Restart করার পর তৈরি হওয়া নতুন Hold-গুলোর ক্ষেত্রে পরিবর্তিত সময়সীমা কার্যকর হবে। আগে তৈরি হওয়া Hold-এর Expiration Time পরিবর্তিত হবে না।

### ১১. CORS Configuration

Frontend এবং Backend আলাদা Port-এ চলার কারণে React Application থেকে API Request পাঠানোর সুবিধার্থে CORS উন্মুক্ত রাখা হয়েছে।

- Frontend: Port `3000`
- Backend API: Port `8080`

এটি Development ও Demonstration Environment-এর জন্য গ্রহণ করা হয়েছে। Production Environment-এ নির্দিষ্ট Trusted Origin অনুমোদন করা উচিত।

### ১২. UTC Timestamp এবং Countdown Synchronization

সকল Timestamp UTC Format-এ সংরক্ষণ করা হয়।

Client Device বা Laptop-এর System Clock ভুল হলেও Countdown যেন যথাসম্ভব সঠিক থাকে, সেজন্য UI API Response-এর সঙ্গে পাওয়া Server Time ব্যবহার করে Client এবং Server-এর Clock Offset হিসাব করে Countdown প্রদর্শন করে।

### ১৩. Automated Testing-এর জন্য আলাদা Database

Automated Integration Test চালানোর জন্য Application Database থেকে আলাদা `school_admission_test` নামে একটি PostgreSQL Database ব্যবহার করা হয়।

এর উদ্দেশ্য হলো Test Data-কে মূল Application Data থেকে আলাদা রাখা এবং Database-Level Transaction ও Concurrency Behavior পরীক্ষা করা।

### ১৪. Test Data এবং Privacy

Development ও Automated Testing-এর সময় Synthetic 17-digit Student ID ব্যবহার করা হবে। কোনো বাস্তব শিক্ষার্থীর ব্যক্তিগত তথ্য বা Birth Registration Number Test Environment-এ ব্যবহার করা উচিত নয়।