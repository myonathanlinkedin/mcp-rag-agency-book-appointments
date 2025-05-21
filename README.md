# **🗓️ AI-Powered Appointment Booking System**  

## **✨ Overview**  
This project is a **cutting-edge AI-driven** appointment booking system designed for **agencies** to schedule appointments, and manage queues in **real-time efficiently**. With **AI Chat Bots**, **Retrieval-Augmented Generation (RAG)**, and **MCP Client/Server integration**, the system enables **context-aware** decision-making using **external knowledge sources**.  

Built with **Domain-Driven Development (DDD)** principles, the system leverages **Apache Kafka & ElasticSearch for appointment indexing**, ensuring **optimized search & performance** across thousands of appointments! The architecture is **microservices-ready**, making it scalable and modular for enterprise adoption. **Now powered by Qdrant Vector DB**, it supports AI-driven **semantic search and retrieval** for enhanced user experiences.  

## 🚀 Key Features

✅ Built-in IdentityServer with Asymmetric JWT Signing 🔐  
✅ AI Chat Bots via PromptAPI 🤖  
✅ Retrieval-augmented generation (RAG) – combines real-time knowledge retrieval with language generation for smarter, context-aware responses 🧠🔍  
✅ Intelligent document parsing using LLM – extracts clean, structured, semantically meaningful text from messy HTML and scanned PDFs 📄🧹🤖  
✅ Users can update the AI brain using RAG by scanning URLs & PDF documents on the fly – parsed content is embedded and stored in Qdrant for semantic search 📄🌐⚡  
✅ RAG with Hangfire for document scan, parse & upload to Qdrant ⚙️  
✅ MCP client/server ready ⚡  
✅ Supports off days & max daily appointments 📅  
✅ Real-time queue grid via API ⏳  
✅ Domain-driven development (DDD) architecture 🏗️  
✅ Event dispatcher for domain events 📨  
✅ Apache Kafka & ElasticSearch for appointment indexing 📡  
✅ Qdrant vector database for AI semantic search 🧠✨  
✅ FluentValidation for validation logic ✅  
✅ Swagger, LINQ, IoC, WebAPI 🛠️  
✅ Automatic email template generation by AI LLM 📧✨  
✅ API with Brain – users can type prompts in natural language 🧠📝  
✅ Microservices-ready, modular & scalable 🏢🔄  
✅ Cloud-ready (Azure/AWS) ☁️  
✅ Refit-powered REST API clients 🔌  
✅ Producer-consumer pattern with buffer cache for real-time insert, save & update 📤📥  
✅ Next.js + React.js chatbot UI – real-time chat interface integrated with backend LLM API 💬⚛️ – supports theme changes  
✅ Redis implementation for faster public key retrieval and chat store caching 🧰🔑

## **📜 Architecture Diagram**  
```plaintext
User → API Gateway → Appointment Service → Event Processing (Kafka) → Search Index (ElasticSearch)  
                  ↳ AI Decision Layer (RAG, MCP Client/Server, Qdrant Vector DB)  
```

## **🔄 User Flow**  
1️⃣ **User registers** on the platform 📝  
2️⃣ **Admin assigns "Agent" role** to the user 👤✅  
3️⃣ **Admin registers an agency** to the system 🏢  
4️⃣ **Agent adds agency users/customers** (who will book appointments) 👥  
5️⃣ **Agent schedules an appointment** for an agency user/customer 📅  
6️⃣ **AI automatically generates an appointment confirmation email template** ✉️🤖  
7️⃣ **Appointment is indexed in Apache Kafka & ElasticSearch for real-time search** 📡  
8️⃣ **User/customer gets notified with details via AI-enhanced email template** 🚀  
9️⃣ **User interacts with AI freely via API with Brain – type any prompt, get smart AI responses** 🧠💬  
🔟 **Qdrant Vector DB enhances search accuracy with AI-powered similarity matching** 🔍💡  
1️⃣1️⃣ **Microservices-ready architecture ensures efficient scaling across multiple agencies** 🏢⚙️  

This ensures a **streamlined booking experience**, allowing agencies to manage **appointments efficiently** with **real-time indexing, AI-generated email templates, and AI-driven semantic search with Qdrant!**  

## 🧰 Tech Stack

🟦 .NET 9 – modern, performant runtime for cloud-native applications  
🛡️ IdentityServer – secure authentication and token issuance  
📅 Hangfire – background job scheduling for asynchronous workflows  
📡 Apache Kafka – distributed event streaming platform  
🔍 ElasticSearch – high-speed, full-text search for appointment indexing  
🧠 Qdrant – vector DB for semantic AI search  
🧾 PromptAPI – LLM-based AI chatbot integration  
🔌 Refit – declarative REST API clients with interface-based contracts  
✅ FluentValidation – fluent rules for robust input validation  
🧪 Swagger / OpenAPI – API documentation and test interface  
☁️ Azure / AWS Ready – cloud-native infrastructure compatible  
📜 Marten DB (PostgreSQL) – event sourcing and document database  
🐘 PostgreSQL – backing store for MartenDB event sourcing and ElasticSearch sync  
🗄️ MS SQL Server – primary application database for transactional data  
🧱 producer/consumer repository pattern – buffer-backed async layer for write-heavy workloads  
⚛️ Next.js / React.js – fast, modern frontend framework for interactive chatbot UI  
🧠 Redis – in-memory cache for fast public key access and general-purpose caching  

## **🛡️ Security & Access Control**  
⚠️ **Strict access policies & authentication layers**  
🔐 **JWT-based authentication**  
🔄 **Audit logs for booking activities**  

## **📬 Contributing**  
We welcome **new features, bug fixes, and performance improvements**. 🚀  
Feel free to submit **pull requests** or open **issues**!  

## **⚡ Future Enhancements**  
🔮 **AI-driven appointment recommendations**  
📢 **Automated notifications for schedule changes**  
📡 **Machine Learning for capacity prediction**  

---

### 📜 License - Apache License 2.0 (TL;DR)

This project follows the **Apache License 2.0**, which means:

- ✅ **You can** use, modify, and distribute the code freely.  
- ✅ **You must** include the original license when distributing.  
- ✅ **You must** include the `NOTICE` file if one is provided.  
- ✅ **You can** use this in personal & commercial projects.  
- ✅ **No warranties** – use at your own risk! 🚀  

For full details, check the [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0).  

---

💡 **This system isn't just another booking tool—it’s an intelligent, scalable AI-powered solution.**  
Let’s **reshape the future** of scheduling with **AI, event-driven processing, scalable microservices, and AI-powered search with Qdrant Vector DB!** 🚀🔥  

---

This project is based on my other project: https://github.com/myonathanlinkedin/productinfo-mcp-rag

---

### 📸 Screenshots of the chatbots
![image](https://github.com/user-attachments/assets/5026cb1c-0a32-40c0-b9e8-ab94668e063b)

![image](https://github.com/user-attachments/assets/02d3a8f3-6661-4e10-a849-5966a3a5bb0f)

![image](https://github.com/user-attachments/assets/df0b57ec-4fab-4f6d-8152-4c477b161639)

![image](https://github.com/user-attachments/assets/0d117c9a-2ac0-446a-b2d3-5275ecbe26e4)

![image](https://github.com/user-attachments/assets/e4ff7d1f-63d0-4a4f-9824-7994ad97aac5)

![image](https://github.com/user-attachments/assets/96a8d654-aeef-492b-9482-a2c6f1b37eab)

![image](https://github.com/user-attachments/assets/42267e9a-2f45-458d-bc2f-f6975b0a17f3)

![image](https://github.com/user-attachments/assets/0f5e3f4c-85c1-45a8-aa93-0f301aede4de)

![image](https://github.com/user-attachments/assets/24ecd2a1-ea6d-4e78-afd4-aa451babcf48)

![image](https://github.com/user-attachments/assets/1ac458dd-9e2a-47e9-93e4-9637c618cc0d)

![image](https://github.com/user-attachments/assets/c0ec5660-e194-4e7b-92fe-9cf2f75d9f7f)







