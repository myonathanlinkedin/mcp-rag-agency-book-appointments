# **🗓️ AI-Powered Appointment Booking System**  

## **✨ Overview**  
This project is a **cutting-edge AI-driven** appointment booking system designed for **agencies** to schedule appointments, and manage queues in **real-time efficiently**. With **AI Chat Bots**, **Retrieval-Augmented Generation (RAG)**, and **MCP Client/Server integration**, the system enables **context-aware** decision-making using **external knowledge sources**.  

Built with **Domain-Driven Development (DDD)** principles, the system leverages **Apache Kafka & ElasticSearch for appointment indexing**, ensuring **optimized search & performance** across thousands of appointments! The architecture is **microservices-ready**, making it scalable and modular for enterprise adoption. **Now powered by Qdrant Vector DB**, it supports AI-driven **semantic search and retrieval** for enhanced user experiences.  

## 🚀 Key Features

✅ Built-in IdentityServer with Asymmetric JWT Signing 🔐  
✅ AI Chat Bots via PromptAPI 🤖  
✅ **Retrieval-Augmented Generation (RAG)** – combines real-time knowledge retrieval with language generation for smarter, context-aware responses 🧠🔍  
✅ **Users can update the AI brain using RAG by scanning URLs & PDF documents on the fly** – parsed content is embedded and stored in Qdrant for semantic search 📄🌐⚡  
✅ RAG with Hangfire for document scan, parse & upload to Qdrant ⚙️  
✅ MCP Client/Server ready ⚡  
✅ Supports Off Days & Max Daily Appointments 📅  
✅ Real-Time Queue Grid via API ⏳  
✅ Domain-Driven Development (DDD) architecture 🏗️  
✅ Event Dispatcher for Domain Events 📨  
✅ Apache Kafka & ElasticSearch for appointment indexing 📡  
✅ Qdrant Vector Database for AI semantic search 🧠✨  
✅ FluentValidation for validation logic ✅  
✅ Swagger, LINQ, IoC, WebAPI 🛠️  
✅ Automatic email template generation by AI LLM 📧✨  
✅ API with Brain – users can type prompts in natural language 🧠📝  
✅ Microservices-ready, modular & scalable 🏢🔄  
✅ Cloud-ready (Azure/AWS) ☁️  
✅ Refit-powered REST API clients 🔌
 
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

## **🧰 Tech Stack**  
🟦 **.NET 9** – Modern, performant runtime for cloud-native applications  
🛡️ **IdentityServer** – Secure authentication and token issuance  
📅 **Hangfire** – Background job scheduling for asynchronous workflows  
📡 **Apache Kafka** – Distributed event streaming platform  
🔍 **ElasticSearch** – High-speed, full-text search for appointment indexing  
🧠 **Qdrant** – Vector DB for semantic AI search  
🧾 **PromptAPI** – LLM-based AI chatbot integration  
🔌 **Refit** – Declarative REST API clients with interface-based contracts  
✅ **FluentValidation** – Fluent rules for robust input validation  
🧪 **Swagger / OpenAPI** – API documentation and test interface  
☁️ **Azure / AWS Ready** – Cloud-native infrastructure compatible  
📜 **Marten DB** – Event sourcing and document database  

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
