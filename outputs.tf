output "alb_url" {
    value = "http://${aws_lb.ecs_alb.dns_name}"
}