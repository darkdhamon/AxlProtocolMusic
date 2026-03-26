# Todo List

## Admin and Auth

- Replace the current development-only bootstrap admin recovery/reset approach with a proper password reset flow that does not require email, SMS, or authenticator app setup.
- Add an Admin Post to Social Media page that lets an admin publish to all configured social media sources at once.

## Content and Media

- Future dated news articles to not show for non-admin/public users.
- Create a default image for news articles when an image is not defined.
- Create a default image for timeline items when an image is not defined.
- Gallery of all uploaded images.
- Add a media library page so uploaded images can be reused instead of pasted by URL.
- Add an admin uploaded image explorer that identifies unused images which can be safely deleted from storage.
- Add upload image support for new items instead of just pasting a URL.
- Add timeline upload image support instead of just pasting a URL.

## Marketing and Communication

- Add a Contact Us page.
- Add newsletter signup and unsubscribe functionality.
- SEO basics: per-page meta descriptions, Open Graph images, sitemap, and robots controls.
- Social posting safety features: draft mode, per-platform toggles, retry status, and failure logs.
- Contact form spam protection and message inbox/admin triage.

## Operations and Reliability

- Automated backups and restore documentation for Mongo/content assets.
- Health checks/startup diagnostics dashboard for database, blob storage, and OpenAI/chatbot integrations.
- Add custom error pages.

## Privacy and Analytics

- A privacy/data retention admin page for visitor analytics cleanup controls.
