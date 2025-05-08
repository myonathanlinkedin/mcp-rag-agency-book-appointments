# **🗓️ AI-Powered Appointment Booking System**  

## **✨ Overview**  
This project is a **cutting-edge AI-driven** appointment booking system designed for **agencies** to schedule appointments, and manage queues in **real-time efficiently**. With **AI Chat Bots**, **Retrieval-Augmented Generation (RAG)**, and **MCP Client/Server integration**, the system enables **context-aware** decision-making using **external knowledge sources**.  

Built with **Domain-Driven Development (DDD)** principles, the system leverages **Apache Kafka & ElasticSearch for appointment indexing**, ensuring **optimized search & performance** across thousands of appointments! The architecture is **microservices-ready**, making it scalable and modular for enterprise adoption. **Now powered by Qdrant Vector DB**, it supports AI-driven **semantic search and retrieval** for enhanced user experiences.  

## **🚀 Key Features**  
✅ **Built-in IdentityServer with Asymmetric JWT Signing** – Secure OAuth2/OpenID Connect token issuance 🔐  
✅ **AI Chat Bots via PromptAPI** for dynamic responses 🤖  
✅ **Retrieval-Augmented Generation (RAG)** for **smart queries** 🔍  
✅ **RAG with Hangfire for Document Scan, Parse & Upload to Qdrant** – Background job orchestration for AI-assisted knowledge ingestion from files 📂⚙️🧠  
✅ **MCP Client/Server Ready** – Context-aware AI-driven decision-making ⚡  
✅ **Supports Off Days & Max Daily Appointments** (overflow handling) 📅  
✅ **Real-Time Queue Grid via API** ⏳  
✅ **Domain-Driven Development (DDD) Architecture** 🏗️  
✅ **Event Dispatcher Mechanism for Domain Events** – Seamlessly propagates changes across bounded contexts, enabling decoupled and reactive business workflows 📨  
✅ **Apache Kafka & ElasticSearch for appointment indexing** 📡  
✅ **Qdrant Vector Database – High-speed AI-powered semantic search** 🧠✨  
✅ **FluentValidation for validation logic** ✅  
✅ **Swagger, LINQ, IoC, and WebAPI implementation** 🛠️  
✅ **Automatic Email Template generation by AI LLM** 📧✨  
✅ **API with Brain – Users can freely type prompts in natural language for AI-driven responses** 🧠📝  
✅ **Microservices-ready – modular, scalable, and adaptable** 🏢🔄  
✅ **Cloud-ready, designed for potential deployment on Azure/AWS** ☁️  
✅ **Refit Integration for REST API consumption** – Simplifies REST API interaction with typed interfaces for seamless API calls 🔌  
 
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

## **🔧 Tech Stack**  
- **C# .NET Core** 🏗️  
- **Entity Framework Core & LINQ** 🔍  
- **FluentValidation for validation logic** ✅  
- **Swagger for API Documentation** 📜  
- **Kafka for Event-Driven Appointment Processing** 🔄  
- **ElasticSearch for Real-Time Appointment Indexing** 🔥  
- **Qdrant Vector Database for AI-driven semantic search** 🧠🔍  
- **AI LLM for Automatic Email Template Generation** 📧🤖  
- **API with Brain – Free Prompt-Based AI Responses** 🧠📝  
- **Microservices-ready with modular services & APIs** 🏢🔄  
- **Cloud-ready for potential Azure/AWS deployment** ☁️  

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

### **📜 License - Apache License 2.0 (TL;DR)**  
This project follows the **Apache License 2.0**, which means:  

✅ **You can** use, modify, and distribute the code freely.  
✅ **You must** include the original license when distributing.  
✅ **You can** use this in personal & commercial projects.  
✅ **No warranties** – use at your own risk! 🚀  

For full details, check the [Apache License 2.0](http://www.apache.org/licenses/LICENSE-2.0).  

---

💡 **This system isn't just another booking tool—it’s an intelligent, scalable AI-powered solution.**  
Let’s **reshape the future** of scheduling with **AI, event-driven processing, scalable microservices, and AI-powered search with Qdrant Vector DB!** 🚀🔥  

---

This project is based on my other project: https://github.com/myonathanlinkedin/productinfo-mcp-rag
