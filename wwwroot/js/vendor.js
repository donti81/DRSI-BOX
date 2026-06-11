/*
{
  name: 'Paces',
  title: 'Paces - Multipurpose Tailwind CSS & Bootstrap Admin Dashboard Template',
  description: 'Paces is a modern, responsive admin dashboard available on ThemeForest. Ideal for building CRM, CMS, project management tools, and custom web applications with a clean UI, flexible layouts, and rich features.',
  author: 'Coderthemes',
  username: 'David Dev',
  keywords: 'Paces, admin dashboard, ThemeForest, Bootstrap 5 admin, responsive admin, CRM dashboard, CMS admin, web app UI, admin theme, premium admin template',
  version: '1.3.0'
}
*/

import "../css/app.css"

import "preline"

import "simplebar"

import { createIcons, icons } from "lucide"
createIcons({ icons })

import moment from "moment"
window.moment = moment